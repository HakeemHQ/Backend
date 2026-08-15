using Hakeem.Application.Configurations;
using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Infrastructure.AI.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class MedicalIntelligenceAgentTests
{
    [Fact]
    public async Task RespondAsync_RecordQuestion_UsesRefinedQueryAndOnlySearchTool()
    {
        var patientId = Guid.NewGuid();
        var searchService = new FakeMedicalRecordSearchService(patientId);
        var chatService = new RoutingToolChatCompletionService(
            """
            {
              "intent": "patient_record_question",
              "query": "current diabetes medications with dose and frequency",
              "focus": null,
              "title": null,
              "language": "en"
            }
            """,
            new Dictionary<string, object?>
            {
                ["query"] =
                    "current diabetes medications with dose and frequency"
            },
            "Your records list Metformin 500 mg twice daily.");
        var agent = CreateAgent(
            searchService,
            new FakeMedicalCvGenerationService(),
            chatService);

        var result = await agent.RespondAsync(
            patientId,
            "Hey, what sugar medicine am I on and how often?",
            CancellationToken.None);

        Assert.Equal(
            MedicalIntelligenceCapability.PatientEvidence,
            result.Capability);
        Assert.Null(result.FocusedMedicalCv);
        var ragResult = Assert.Single(result.RagResults);
        Assert.Equal("Medication", ragResult.RecordType);
        Assert.Equal(0.85f, ragResult.Score);
        Assert.Equal(
            "current diabetes medications with dose and frequency",
            searchService.Query);
        Assert.Equal(patientId, searchService.PatientId);
        Assert.Equal(15, searchService.Limit);
        Assert.Equal(
            ["search_patient_medical_records"],
            chatService.AvailableToolNames);
        Assert.Equal(1, chatService.ToolCallCount);
        Assert.Equal(
            "Your records list Metformin 500 mg twice daily.",
            result.Message);
        Assert.IsType<RequiredFunctionChoiceBehavior>(
            chatService.ToolExecutionSettings!.FunctionChoiceBehavior);
    }

    [Fact]
    public async Task RespondAsync_FocusedCvAction_UsesRefinedArgumentsAndReturnsMetadata()
    {
        var patientId = Guid.NewGuid();
        var generationService = new FakeMedicalCvGenerationService();
        var searchService = new FakeMedicalRecordSearchService(patientId);
        var chatService = new RoutingToolChatCompletionService(
            """
            {
              "intent": "focused_cv_action",
              "query": null,
              "focus": "Diabetes",
              "title": "Diabetes Medical CV",
              "language": "en"
            }
            """,
            new Dictionary<string, object?>
            {
                ["focus"] = "Diabetes",
                ["title"] = "Diabetes Medical CV",
                ["language"] = "en"
            },
            "Diabetes Medical CV, version 1, was created as Draft.");
        var agent = CreateAgent(
            searchService,
            generationService,
            chatService);

        var result = await agent.RespondAsync(
            patientId,
            "Please make me a CV about my sugar condition.",
            CancellationToken.None);

        Assert.Equal(
            MedicalIntelligenceCapability.FocusedMedicalCv,
            result.Capability);
        Assert.Equal(
            ["generate_focused_medical_cv"],
            chatService.AvailableToolNames);
        Assert.Equal(1, chatService.ToolCallCount);
        Assert.NotNull(result.FocusedMedicalCv);
        Assert.Empty(result.RagResults);
        Assert.Equal("Diabetes Medical CV", result.FocusedMedicalCv.Title);
        Assert.Equal("Diabetes", generationService.Focus);
        Assert.Equal("Diabetes Medical CV", generationService.Title);
        Assert.Equal("en", generationService.Language);
        Assert.Equal(1, chatService.NonToolCallCount);
        Assert.Equal(
            "Diabetes Medical CV was created successfully as version 1 " +
            "(Draft). The preview link is included in the response.",
            result.Message);
        Assert.Contains(
            searchService.Queries,
            query => query.Contains("medications"));
        Assert.Contains("Diabetes", searchService.Queries);
    }

    [Fact]
    public async Task RespondAsync_ArabicFocusedCvRequest_PropagatesArabicWithoutTranslatingFocus()
    {
        var patientId = Guid.NewGuid();
        var generationService = new FakeMedicalCvGenerationService();
        var searchService = new FakeMedicalRecordSearchService(patientId);
        var chatService = new RoutingToolChatCompletionService(
            """
            {
              "intent": "focused_cv_action",
              "query": null,
              "focus": "Diabetes",
              "title": "سيرة طبية لمرض السكري",
              "language": "ar"
            }
            """,
            new Dictionary<string, object?>
            {
                ["focus"] = "Diabetes",
                ["title"] = "سيرة طبية لمرض السكري",
                ["language"] = "ar"
            },
            "unused");
        var agent = CreateAgent(searchService, generationService, chatService);

        var result = await agent.RespondAsync(
            patientId,
            "أنشئ Medical CV لمرض السكري",
            CancellationToken.None);

        Assert.Equal("ar", generationService.Language);
        Assert.Equal("Diabetes", generationService.Focus);
        Assert.Equal("سيرة طبية لمرض السكري", result.FocusedMedicalCv!.Title);
        Assert.Equal(
            "تم إنشاء سيرة طبية لمرض السكري بنجاح كإصدار رقم 1 " +
            "(مسودة). رابط المعاينة مرفق في الاستجابة.",
            result.Message);
        Assert.Contains("Diabetes", searchService.Queries);
        Assert.Contains(
            "medications and treatments used for Diabetes",
            searchService.Queries);
    }

    [Fact]
    public async Task RespondAsync_OutOfScope_RefusesWithoutToolCall()
    {
        var patientId = Guid.NewGuid();
        var chatService = new RoutingToolChatCompletionService(
            """
            {
              "intent": "out_of_scope",
              "query": null,
              "focus": null,
              "title": null,
              "language": "en"
            }
            """,
            new Dictionary<string, object?>(),
            "unused");
        var agent = CreateAgent(
            new FakeMedicalRecordSearchService(patientId),
            new FakeMedicalCvGenerationService(),
            chatService);

        var result = await agent.RespondAsync(
            patientId,
            "Should I double my medication dose?",
            CancellationToken.None);

        Assert.Equal(MedicalIntelligenceCapability.None, result.Capability);
        Assert.Empty(result.RagResults);
        Assert.Contains("only answer questions", result.Message);
        Assert.Equal(0, chatService.ToolCallCount);
        Assert.Empty(chatService.AvailableToolNames);
        Assert.Equal(1, chatService.NonToolCallCount);
    }

    [Fact]
    public async Task RespondAsync_ArabicOutOfScope_ReturnsLocalizedResponse()
    {
        var patientId = Guid.NewGuid();
        var chatService = new RoutingToolChatCompletionService(
            """
            {
              "intent": "out_of_scope",
              "query": null,
              "focus": null,
              "title": null,
              "language": "ar"
            }
            """,
            new Dictionary<string, object?>(),
            "unused");
        var agent = CreateAgent(
            new FakeMedicalRecordSearchService(patientId),
            new FakeMedicalCvGenerationService(),
            chatService);

        var result = await agent.RespondAsync(
            patientId,
            "هل يجب أن أضاعف جرعة الدواء؟",
            CancellationToken.None);

        Assert.Equal(
            "يمكنني فقط الإجابة عن الأسئلة باستخدام سجلاتك الطبية أو " +
            "إنشاء سيرة طبية مركزة من تلك السجلات.",
            result.Message);
        Assert.Equal(MedicalIntelligenceCapability.None, result.Capability);
        Assert.Equal(0, chatService.ToolCallCount);
    }

    [Fact]
    public async Task RespondAsync_MissingFocusedCvFocus_AsksForClarification()
    {
        var patientId = Guid.NewGuid();
        var chatService = new RoutingToolChatCompletionService(
            """
            {
              "intent": "focused_cv_action",
              "query": null,
              "focus": null,
              "title": null,
              "language": "en"
            }
            """,
            new Dictionary<string, object?>(),
            "unused");
        var agent = CreateAgent(
            new FakeMedicalRecordSearchService(patientId),
            new FakeMedicalCvGenerationService(),
            chatService);

        var result = await agent.RespondAsync(
            patientId,
            "Create a focused CV for me.",
            CancellationToken.None);

        Assert.Equal(MedicalIntelligenceCapability.None, result.Capability);
        Assert.Contains("specific topic", result.Message);
        Assert.Equal(0, chatService.ToolCallCount);
    }

    private static MedicalIntelligenceAgent CreateAgent(
        IMedicalRecordSearchService searchService,
        IMedicalCvGenerationService generationService,
        IChatCompletionService chatService) =>
        new(
            searchService,
            new FakeMedicalRecordsRepository(),
            generationService,
            chatService,
            Options.Create(
                new MedicalIntelligenceAgentConfiguration
                {
                    SearchLimit = 15,
                    FocusedCvMinimumScore = 0.5f,
                    FocusedCvRelatedEvidenceMinimumScore = 0.35f
                }),
            Options.Create(
                new GeminiChatConfiguration
                {
                    MaxTokens = 4_000,
                    AgentTimeoutSeconds = 30
                }),
            NullLoggerFactory.Instance,
            NullLogger<MedicalIntelligenceAgent>.Instance);

    private sealed class RoutingToolChatCompletionService(
        string routeJson,
        IReadOnlyDictionary<string, object?> toolArguments,
        string finalResponse)
        : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes { get; } =
            new Dictionary<string, object?>();

        public int NonToolCallCount { get; private set; }
        public int ToolCallCount { get; private set; }
        public List<string> AvailableToolNames { get; } = [];
        public GeminiPromptExecutionSettings? ToolExecutionSettings
            { get; private set; }

        public async Task<IReadOnlyList<ChatMessageContent>>
            GetChatMessageContentsAsync(
                ChatHistory chatHistory,
                PromptExecutionSettings? executionSettings = null,
                Kernel? kernel = null,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (kernel is null)
            {
                NonToolCallCount++;
                var content = NonToolCallCount == 1
                    ? routeJson
                    : finalResponse;

                return
                [
                    new ChatMessageContent(AuthorRole.Assistant, content)
                ];
            }

            ToolCallCount++;
            ToolExecutionSettings = Assert.IsType<GeminiPromptExecutionSettings>(
                executionSettings);
            var function = Assert.Single(
                kernel.Plugins.SelectMany(plugin => plugin));
            AvailableToolNames.Add(function.Name);
            var arguments = new KernelArguments();

            foreach (var (name, value) in toolArguments)
            {
                arguments[name] = value;
            }

            await function.InvokeAsync(
                kernel,
                arguments,
                cancellationToken);

            return
            [
                new ChatMessageContent(
                    AuthorRole.Assistant,
                    "Tool invocation completed.")
            ];
        }

        public IAsyncEnumerable<StreamingChatMessageContent>
            GetStreamingChatMessageContentsAsync(
                ChatHistory chatHistory,
                PromptExecutionSettings? executionSettings = null,
                Kernel? kernel = null,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeMedicalRecordSearchService(Guid patientId)
        : IMedicalRecordSearchService
    {
        public string? Query { get; private set; }
        public Guid PatientId { get; private set; }
        public int Limit { get; private set; }
        public List<string> Queries { get; } = [];

        public Task<IReadOnlyList<MedicalRecordSearchResult>> SearchAsync(
            string query,
            Guid patientProfileId,
            int limit,
            CancellationToken cancellationToken)
        {
            Query = query;
            Queries.Add(query);
            PatientId = patientProfileId;
            Limit = limit;

            IReadOnlyList<MedicalRecordSearchResult> results =
            [
                new(
                    Guid.NewGuid(),
                    patientId,
                    0.85f,
                    "Medication",
                    "MedicationName: Metformin, Dose: 500 mg",
                    "Confirmed",
                    new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    "MedicationName: Metformin; Dose: 500 mg; Frequency: twice daily",
                    "MedicationName: Metformin; Dose: 500 mg; Frequency: twice daily")
            ];

            return Task.FromResult(results);
        }
    }

    private sealed class FakeMedicalRecordsRepository
        : IMedicalRecordsRepository
    {
        public Task<IReadOnlyList<MedicalRecord>>
            GetConfirmedByRecordTypeAsync(
                Guid patientProfileId,
                string recordType,
                CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MedicalRecord>>([]);

        public Task<IReadOnlyList<MedicalRecord>> GetAllConfirmedAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PaginatedResult<MedicalRecord>> GetMedicalRecordsAsync(
            Guid patientProfileId,
            string? search,
            string? recordType,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(MedicalRecord record) =>
            throw new NotSupportedException();

        public Task<MedicalRecord?> GetBySourceExtractedItemIdAsync(
            Guid extractedItemId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MedicalRecord?> GetMedicalRecordByIdAsync(
            Guid medicalRecordId,
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MedicalRecord?> GetByIdWithFieldsAsync(
            Guid medicalRecordId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MedicalRecord?> GetByIdWithDetailsAsync(
            Guid medicalRecordId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeMedicalCvGenerationService
        : IMedicalCvGenerationService
    {
        public string? Focus { get; private set; }
        public string? Title { get; private set; }
        public string? Language { get; private set; }

        public Task<MedicalCvGenerationResult> GenerateFullAsync(
            Guid patientId,
            string title,
            string language,
            MedicalCvCreatedByRole createdByRole,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MedicalCvGenerationResult> GenerateFocusedAsync(
            Guid patientId,
            string focus,
            string title,
            IReadOnlyList<MedicalCvEvidenceItem> evidence,
            string language,
            MedicalCvCreatedByRole createdByRole,
            CancellationToken cancellationToken = default)
        {
            Focus = focus;
            Title = title;
            Language = language;

            return Task.FromResult(new MedicalCvGenerationResult(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                title,
                1,
                MedicalCvScopeType.Focused,
                focus,
                "internal/file.pdf",
                MedicalCvVersionStatus.Draft,
                new DateTime(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc)));
        }
    }
}
