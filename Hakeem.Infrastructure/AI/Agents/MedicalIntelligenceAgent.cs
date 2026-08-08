using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Infrastructure.AI.Agents.Plugins;
using Hakeem.Infrastructure.AI.Agents.Prompts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Hakeem.Infrastructure.AI.Agents;

public sealed class MedicalIntelligenceAgent(
    IMedicalRecordSearchService medicalRecordSearchService,
    IMedicalRecordsRepository medicalRecordsRepository,
    IMedicalCvGenerationService medicalCvGenerationService,
    IChatCompletionService chatCompletionService,
    IOptions<MedicalIntelligenceAgentConfiguration> medicalOptions,
    IOptions<GeminiChatConfiguration> geminiOptions,
    ILoggerFactory loggerFactory,
    ILogger<MedicalIntelligenceAgent> logger)
    : IMedicalIntelligenceAgent
{
    private const string PluginName = "medicalintelligence";

    private static readonly JsonSerializerOptions RouteJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly GeminiChatConfiguration _geminiConfiguration =
        geminiOptions.Value;

    public async Task<MedicalIntelligenceResponse> RespondAsync(
        Guid patientId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (patientId == Guid.Empty)
        {
            throw new ArgumentException(
                "A patient ID is required.",
                nameof(patientId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        cancellationToken.ThrowIfCancellationRequested();

        using var timeoutSource = new CancellationTokenSource(
            TimeSpan.FromSeconds(
                _geminiConfiguration.AgentTimeoutSeconds));
        using var linkedSource = CancellationTokenSource
            .CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);

        try
        {
            var route = await RouteAsync(
                message.Trim(),
                linkedSource.Token);

            if (route.Intent == MedicalIntelligenceIntent.OutOfScope)
            {
                return new MedicalIntelligenceResponse(
                    MedicalIntelligenceAgentPrompt.OutOfScopeResponse,
                    MedicalIntelligenceCapability.None,
                    []);
            }

            if (route.Intent == MedicalIntelligenceIntent.NeedsClarification)
            {
                return new MedicalIntelligenceResponse(
                    MedicalIntelligenceAgentPrompt.ClarificationResponse,
                    MedicalIntelligenceCapability.None,
                    []);
            }

            return await ExecuteSupportedRequestAsync(
                patientId,
                message.Trim(),
                route,
                linkedSource.Token);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested &&
                  timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Medical-intelligence processing exceeded the {_geminiConfiguration.AgentTimeoutSeconds}-second timeout.",
                exception);
        }
    }

    private async Task<MedicalIntelligenceResponse>
        ExecuteSupportedRequestAsync(
            Guid patientId,
            string originalMessage,
            MedicalIntelligenceRoute route,
            CancellationToken cancellationToken)
    {
        var plugin = new MedicalIntelligencePlugin(
            patientId,
            medicalRecordSearchService,
            medicalRecordsRepository,
            medicalCvGenerationService,
            medicalOptions,
            loggerFactory.CreateLogger<MedicalIntelligencePlugin>());
        var kernelPlugin = KernelPluginFactory.CreateFromObject(
            plugin,
            PluginName);

        var functionName = route.Intent switch
        {
            MedicalIntelligenceIntent.PatientRecordQuestion =>
                "search_patient_medical_records",
            MedicalIntelligenceIntent.FocusedCvAction =>
                "generate_focused_medical_cv",
            _ => throw new InvalidOperationException(
                "Only supported medical-intelligence routes can invoke tools.")
        };
        var requiredFunction = kernelPlugin[functionName];
        var invocationFilter = new SingleToolInvocationFilter(logger);
        var kernel = CreateToolKernel(
            requiredFunction,
            invocationFilter);
        var toolAgent = CreateToolAgent(kernel, requiredFunction);
        var toolArguments = BuildToolArguments(route);

        await InvokeRequiredToolAsync(
            toolAgent,
            MedicalIntelligenceAgentPrompt.BuildToolInvocationMessage(
                toolArguments),
            cancellationToken);

        object toolResult;
        MedicalIntelligenceCapability capability;
        IReadOnlyList<PatientMedicalEvidenceItem> ragResults = [];
        FocusedMedicalCvActionResult? focusedMedicalCv = null;

        if (route.Intent == MedicalIntelligenceIntent.PatientRecordQuestion)
        {
            if (!string.Equals(
                    plugin.LastPatientEvidenceQuery,
                    route.Query,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The patient-evidence tool was not invoked with the normalized query.");
            }

            var evidenceResult = plugin.LastPatientEvidenceResult
                ?? throw new InvalidDataException(
                    "The patient-evidence tool did not return a result.");
            toolResult = evidenceResult;
            ragResults = evidenceResult.Records;
            capability = MedicalIntelligenceCapability.PatientEvidence;
        }
        else
        {
            if (!string.Equals(
                    plugin.LastFocusedMedicalCvFocus,
                    route.Focus,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    plugin.LastFocusedMedicalCvTitle,
                    route.Title,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "The focused-medical-CV tool was not invoked with the normalized arguments.");
            }

            var focusedResult = plugin.LastFocusedMedicalCvResult
                ?? throw new InvalidDataException(
                    "The focused-medical-CV tool did not return a result.");
            toolResult = focusedResult;
            focusedMedicalCv = focusedResult.MedicalCv;
            capability = MedicalIntelligenceCapability.FocusedMedicalCv;
        }

        var responseMessage = route.Intent ==
                              MedicalIntelligenceIntent.FocusedCvAction
            ? BuildFocusedCvResponseMessage(
                (FocusedMedicalCvToolResult)toolResult)
            : await GenerateGroundedResponseAsync(
                originalMessage,
                toolResult,
                cancellationToken);

        return new MedicalIntelligenceResponse(
            responseMessage,
            capability,
            ragResults,
            focusedMedicalCv);
    }

    private static string BuildFocusedCvResponseMessage(
        FocusedMedicalCvToolResult result)
    {
        if (!result.Success)
        {
            return result.Message;
        }

        var cv = result.MedicalCv
            ?? throw new InvalidDataException(
                "The focused-medical-CV tool reported success without CV metadata.");

        return $"{cv.Title} was created successfully as version " +
               $"{cv.VersionNumber} ({cv.Status}). The preview link is " +
               "included in the response.";
    }

    private async Task<MedicalIntelligenceRoute> RouteAsync(
        string message,
        CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(
            MedicalIntelligenceAgentPrompt.RoutingSystem);
        history.AddUserMessage(
            MedicalIntelligenceAgentPrompt.BuildRoutingMessage(message));

        var settings = new GeminiPromptExecutionSettings
        {
            MaxTokens = Math.Min(_geminiConfiguration.MaxTokens, 1_000),
            Temperature = 0,
            ResponseMimeType = "application/json",
            ResponseSchema = BuildRouteResponseSchema()
        };
        var response = await chatCompletionService.GetChatMessageContentAsync(
            history,
            settings,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            throw new InvalidDataException(
                "The medical-intelligence router returned an empty response.");
        }

        MedicalIntelligenceRouteDecision? decision;
        try
        {
            decision = JsonSerializer.Deserialize<MedicalIntelligenceRouteDecision>(
                ExtractJsonPayload(response.Content),
                RouteJsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The medical-intelligence router returned invalid JSON.",
                exception);
        }

        return ValidateRoute(decision);
    }

    private async Task<string> GenerateGroundedResponseAsync(
        string originalMessage,
        object toolResult,
        CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(
            MedicalIntelligenceAgentPrompt.ResponseSystem);
        history.AddUserMessage(
            MedicalIntelligenceAgentPrompt.BuildResponseMessage(
                originalMessage,
                toolResult));

        var settings = new GeminiPromptExecutionSettings
        {
            MaxTokens = _geminiConfiguration.MaxTokens,
            Temperature = 0
        };
        var response = await chatCompletionService.GetChatMessageContentAsync(
            history,
            settings,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            throw new InvalidDataException(
                "The medical-intelligence agent returned an empty response.");
        }

        return response.Content.Trim();
    }

    private Kernel CreateToolKernel(
        KernelFunction requiredFunction,
        SingleToolInvocationFilter invocationFilter)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatCompletionService);
        var kernel = builder.Build();
        kernel.Plugins.AddFromFunctions(
            PluginName,
            [requiredFunction]);
        kernel.AutoFunctionInvocationFilters.Add(invocationFilter);

        return kernel;
    }

    private ChatCompletionAgent CreateToolAgent(
        Kernel kernel,
        KernelFunction requiredFunction)
    {
        var settings = new GeminiPromptExecutionSettings
        {
            MaxTokens = _geminiConfiguration.MaxTokens,
            Temperature = 0,
#pragma warning disable CS0618
            ToolCallBehavior =
                GeminiToolCallBehavior.AutoInvokeKernelFunctions,
#pragma warning restore CS0618
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(
                [requiredFunction],
                autoInvoke: true,
                new FunctionChoiceBehaviorOptions
                {
                    AllowConcurrentInvocation = false,
                    AllowParallelCalls = false,
                    AllowStrictSchemaAdherence = true
                })
        };

        return new ChatCompletionAgent
        {
            Name = "MedicalIntelligenceToolAgent",
            Instructions =
                MedicalIntelligenceAgentPrompt.ToolInvocationSystem,
            Kernel = kernel,
            Arguments = new KernelArguments(settings)
        };
    }

    private static async Task InvokeRequiredToolAsync(
        ChatCompletionAgent agent,
        string prompt,
        CancellationToken cancellationToken)
    {
        await foreach (var _ in agent.InvokeAsync(
                           prompt,
                           cancellationToken: cancellationToken))
        {
            // The single-tool filter captures the result and terminates this
            // phase before arbitrary model text can be used.
        }
    }

    private static object BuildToolArguments(MedicalIntelligenceRoute route) =>
        route.Intent switch
        {
            MedicalIntelligenceIntent.PatientRecordQuestion => new
            {
                query = route.Query
            },
            MedicalIntelligenceIntent.FocusedCvAction => new
            {
                focus = route.Focus,
                title = route.Title
            },
            _ => throw new InvalidOperationException(
                "An unsupported route cannot produce tool arguments.")
        };

    private static MedicalIntelligenceRoute ValidateRoute(
        MedicalIntelligenceRouteDecision? decision)
    {
        if (decision is null || string.IsNullOrWhiteSpace(decision.Intent))
        {
            throw new InvalidDataException(
                "The medical-intelligence router returned an invalid decision.");
        }

        var intent = decision.Intent.Trim().ToLowerInvariant() switch
        {
            "patient_record_question" =>
                MedicalIntelligenceIntent.PatientRecordQuestion,
            "focused_cv_action" =>
                MedicalIntelligenceIntent.FocusedCvAction,
            "needs_clarification" =>
                MedicalIntelligenceIntent.NeedsClarification,
            "out_of_scope" =>
                MedicalIntelligenceIntent.OutOfScope,
            _ => throw new InvalidDataException(
                "The medical-intelligence router returned an unknown intent.")
        };

        if (intent == MedicalIntelligenceIntent.PatientRecordQuestion)
        {
            if (string.IsNullOrWhiteSpace(decision.Query))
            {
                throw new InvalidDataException(
                    "The patient-record route did not include a search query.");
            }

            return new MedicalIntelligenceRoute(
                intent,
                decision.Query.Trim(),
                null,
                null);
        }

        if (intent == MedicalIntelligenceIntent.FocusedCvAction)
        {
            if (string.IsNullOrWhiteSpace(decision.Focus))
            {
                return new MedicalIntelligenceRoute(
                    MedicalIntelligenceIntent.NeedsClarification,
                    null,
                    null,
                    null);
            }

            var focus = decision.Focus.Trim();
            var title = string.IsNullOrWhiteSpace(decision.Title)
                ? $"{focus} Medical CV"
                : decision.Title.Trim();
            return new MedicalIntelligenceRoute(
                intent,
                null,
                focus,
                title);
        }

        return new MedicalIntelligenceRoute(
            intent,
            null,
            null,
            null);
    }

    private static string ExtractJsonPayload(string content)
    {
        var trimmed = content.Trim();
        var start = trimmed.IndexOf('{', StringComparison.Ordinal);
        var end = trimmed.LastIndexOf('}');

        return start >= 0 && end >= start
            ? trimmed[start..(end + 1)]
            : trimmed;
    }

    private static JsonObject BuildRouteResponseSchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["properties"] = new JsonObject
        {
            ["intent"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray
                {
                    "patient_record_question",
                    "focused_cv_action",
                    "needs_clarification",
                    "out_of_scope"
                }
            },
            ["query"] = new JsonObject
            {
                ["type"] = new JsonArray { "string", "null" }
            },
            ["focus"] = new JsonObject
            {
                ["type"] = new JsonArray { "string", "null" }
            },
            ["title"] = new JsonObject
            {
                ["type"] = new JsonArray { "string", "null" }
            }
        },
        ["required"] = new JsonArray
        {
            "intent",
            "query",
            "focus",
            "title"
        }
    };

    private sealed class MedicalIntelligenceRouteDecision
    {
        public string? Intent { get; init; }
        public string? Query { get; init; }
        public string? Focus { get; init; }
        public string? Title { get; init; }
    }

    private sealed record MedicalIntelligenceRoute(
        MedicalIntelligenceIntent Intent,
        string? Query,
        string? Focus,
        string? Title);

    private enum MedicalIntelligenceIntent
    {
        PatientRecordQuestion,
        FocusedCvAction,
        NeedsClarification,
        OutOfScope
    }
}
