using System.Text.Json;
using Hakeem.Application.Features.MedicalDocuments.DTOs;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction;

public sealed class DocumentExtractionResultSerializationTests
{
    [Fact]
    public void Serialize_LabReport_UsesCanonicalStringValue()
    {
        var result = new DocumentExtractionResult(
            MedicalDocumentType.LabReport,
            []);

        var json = JsonSerializer.Serialize(result);

        Assert.Contains("\"documentType\":\"LabReport\"", json);
    }

    [Fact]
    public void Deserialize_NonCanonicalDocumentType_ThrowsJsonException()
    {
        const string json =
            """
            {
              "documentType": "Blood Report",
              "items": []
            }
            """;

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<DocumentExtractionResult>(json));
    }
}
