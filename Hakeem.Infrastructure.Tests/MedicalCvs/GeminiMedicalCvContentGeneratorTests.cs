using System.Text.Json;
using System.Text.Json.Nodes;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Infrastructure.AI.MedicalCvs;
using Hakeem.Infrastructure.Tests.AI.Agents.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class GeminiMedicalCvContentGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_UsesDedicatedLimitAndCountAgnosticSchema()
    {
        var chatService = new FakeChatCompletionService(
            ValidSingleRecordResponse());
        var generator = new GeminiMedicalCvContentGenerator(
            chatService,
            Options.Create(new GeminiChatConfiguration
            {
                MaxTokens = 4_000,
                MedicalCvMaxTokens = 16_384
            }),
            NullLogger<GeminiMedicalCvContentGenerator>.Instance);

        await generator.GenerateAsync(CreateRequest());

        var settings = Assert.IsType<GeminiPromptExecutionSettings>(
            chatService.ReceivedExecutionSettings);
        Assert.Equal(16_384, settings.MaxTokens);
        var schema = Assert.IsType<JsonObject>(settings.ResponseSchema);
        var properties = Assert.IsType<JsonObject>(schema["properties"]);
        var records = Assert.IsType<JsonObject>(properties["records"]);
        Assert.Null(records["minItems"]);
        Assert.Null(records["maxItems"]);
        var items = Assert.IsType<JsonObject>(records["items"]);
        var itemProperties = Assert.IsType<JsonObject>(items["properties"]);
        var recordIndex = Assert.IsType<JsonObject>(
            itemProperties["recordIndex"]);
        Assert.Null(recordIndex["minimum"]);
        Assert.Null(recordIndex["maximum"]);
    }

    [Fact]
    public async Task GenerateAsync_WithManyRecords_UsesStableSchemaAndValidatesResponse()
    {
        const int recordCount = 22;
        var chatService = new FakeChatCompletionService(
            ValidResponse(recordCount));
        var generator = new GeminiMedicalCvContentGenerator(
            chatService,
            Options.Create(new GeminiChatConfiguration
            {
                MedicalCvMaxTokens = 16_384
            }),
            NullLogger<GeminiMedicalCvContentGenerator>.Instance);

        var content = await generator.GenerateAsync(
            CreateRequest(
                Enumerable.Range(0, recordCount)
                    .Select(index => $"Confirmed record {index}")
                    .ToArray()));

        Assert.Equal(recordCount, content.Sections.Sum(
            section => section.Entries.Count));

        var settings = Assert.IsType<GeminiPromptExecutionSettings>(
            chatService.ReceivedExecutionSettings);
        var schema = Assert.IsType<JsonObject>(settings.ResponseSchema);
        var properties = Assert.IsType<JsonObject>(schema["properties"]);
        var records = Assert.IsType<JsonObject>(properties["records"]);
        Assert.Null(records["minItems"]);
        Assert.Null(records["maxItems"]);
    }

    [Fact]
    public async Task GenerateAsync_WhenJsonIsTruncated_ReturnsServiceUnavailable()
    {
        var generator = new GeminiMedicalCvContentGenerator(
            new FakeChatCompletionService(
                "{\"summary\":\"Confirmed history without a closing quote"),
            Options.Create(new GeminiChatConfiguration
            {
                MedicalCvMaxTokens = 16_384
            }),
            NullLogger<GeminiMedicalCvContentGenerator>.Instance);

        var exception = await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => generator.GenerateAsync(CreateRequest()));

        Assert.Equal(ErrorCodes.MedicalCvAiUnavailable, exception.ErrorCode);
    }

    [Fact]
    public async Task GenerateAsync_AcceptsCommonGeminiJsonVariations()
    {
        var generator = new GeminiMedicalCvContentGenerator(
            new FakeChatCompletionService(
                """
                ```json
                {
                  "summary": "Confirmed history",
                  "records": [
                    {
                      "recordIndex": 0,
                      "section": "Visits",
                      "title": "Confirmed visit",
                      "date": "2026-08-07",
                      "details": ["Status: Confirmed"]
                    }
                  ],
                  "additionalNote": "ignored",
                }
                ```
                """),
            Options.Create(new GeminiChatConfiguration { MaxTokens = 2_000 }),
            NullLogger<GeminiMedicalCvContentGenerator>.Instance);

        var content = await generator.GenerateAsync(CreateRequest());

        Assert.Equal("Medical CV - Patient", content.Title);
        Assert.Equal("Confirmed history", content.Summary);
        var section = Assert.Single(content.Sections);
        Assert.Equal("Visits", section.Heading);
        var entry = Assert.Single(section.Entries);
        Assert.Equal("Confirmed visit", entry.Title);
        Assert.Equal("2026-08-07", entry.Date);
        Assert.Equal("Status: Confirmed", Assert.Single(entry.Details));
    }

    [Fact]
    public async Task GenerateAsync_OrganizesMedicationAndAllergyDisplayNames()
    {
        var generator = new GeminiMedicalCvContentGenerator(
            new FakeChatCompletionService(
                """
                {
                  "summary": "Confirmed medications and allergy information are listed below.",
                  "records": [
                    {
                      "recordIndex": 0,
                      "section": "Medications",
                      "title": "Paracetamol",
                      "date": "2026-08-07",
                      "details": ["Dose: 500 mg", "Route: by mouth", "Frequency: every 6 hours as needed for fever or pain"]
                    },
                    {
                      "recordIndex": 1,
                      "section": "Allergies",
                      "title": "Penicillin",
                      "date": null,
                      "details": []
                    },
                    {
                      "recordIndex": 2,
                      "section": "Medications",
                      "title": "Vitamin D3",
                      "date": null,
                      "details": ["Dose: 2000 only", "Frequency: once daily", "Route: Oral"]
                    }
                  ]
                }
                """),
            Options.Create(new GeminiChatConfiguration()),
            NullLogger<GeminiMedicalCvContentGenerator>.Instance);

        var request = CreateRequest(
        [
            "MedicationName: Paracetamol, Dose: 500 mg, Route: by mouth, Frequency: every 6 hours as needed for fever or pain",
            "AllergyName: Penicillin",
            "MedicationName: Vitamin D3, Dose: 2000 only, Frequency: once daily, Route: Oral"
        ]);

        var content = await generator.GenerateAsync(request);

        Assert.Equal(2, content.Sections.Count);
        var medications = Assert.Single(
            content.Sections,
            section => section.Heading == "Medications");
        Assert.Equal(2, medications.Entries.Count);
        Assert.Equal("Paracetamol", medications.Entries[0].Title);
        Assert.Equal("Vitamin D3", medications.Entries[1].Title);
        var allergies = Assert.Single(
            content.Sections,
            section => section.Heading == "Allergies");
        Assert.Equal("Penicillin", Assert.Single(allergies.Entries).Title);
    }

    [Fact]
    public async Task GenerateAsync_WhenRecordIndexIsDuplicated_ReturnsServiceUnavailable()
    {
        var generator = new GeminiMedicalCvContentGenerator(
            new FakeChatCompletionService(
                """
                {
                  "summary": "Confirmed history.",
                  "records": [
                    { "recordIndex": 0, "section": "Visits", "title": "One", "date": null, "details": [] },
                    { "recordIndex": 0, "section": "Visits", "title": "Two", "date": null, "details": [] }
                  ]
                }
                """),
            Options.Create(new GeminiChatConfiguration()),
            NullLogger<GeminiMedicalCvContentGenerator>.Instance);

        var exception = await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => generator.GenerateAsync(CreateRequest(["One", "Two"])));

        Assert.Equal(ErrorCodes.MedicalCvAiUnavailable, exception.ErrorCode);
    }

    [Fact]
    public async Task GenerateAsync_WhenChatServiceFails_ReturnsServiceUnavailable()
    {
        var generator = new GeminiMedicalCvContentGenerator(
            new FakeChatCompletionService(new HttpRequestException("Unavailable")),
            Options.Create(new GeminiChatConfiguration { MaxTokens = 2_000 }),
            NullLogger<GeminiMedicalCvContentGenerator>.Instance);
        var request = CreateRequest();

        var exception = await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => generator.GenerateAsync(request));

        Assert.Equal(ErrorCodes.MedicalCvAiUnavailable, exception.ErrorCode);
        Assert.Equal(503, exception.StatusCode);
    }

    private static MedicalCvContentRequest CreateRequest(
        IReadOnlyList<string>? displayNames = null)
    {
        displayNames ??= ["Confirmed record"];

        return new MedicalCvContentRequest(
            new MedicalCvPatientInformation(
                "Patient",
                new DateTime(1990, 1, 1),
                "Male",
                "patient@example.com",
                "+201000000000"),
            MedicalCvScopeType.Full,
            null,
            displayNames.Select((displayName, index) =>
                new MedicalCvEvidenceItem(
                    Guid.NewGuid(),
                    null,
                    displayName,
                    "Visit",
                    index == 0 ? "2026-08-07" : null))
                .ToArray());
    }

    private static string ValidSingleRecordResponse()
    {
        return """
            {
              "summary": "Confirmed history",
              "records": [
                {
                  "recordIndex": 0,
                  "section": "Visits",
                  "title": "Confirmed visit",
                  "date": "2026-08-07",
                  "details": ["Status: Confirmed"]
                }
              ]
            }
            """;
    }

    private static string ValidResponse(int recordCount) =>
        JsonSerializer.Serialize(new
        {
            summary = "Confirmed medical history",
            records = Enumerable.Range(0, recordCount).Select(index => new
            {
                recordIndex = index,
                section = "Visits",
                title = $"Confirmed record {index}",
                date = (string?)null,
                details = Array.Empty<string>()
            })
        });
}
