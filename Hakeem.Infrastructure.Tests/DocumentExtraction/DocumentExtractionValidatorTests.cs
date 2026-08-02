using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Services.DocumentExtraction;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction;

public sealed class DocumentExtractionValidatorTests
{
    private readonly DocumentExtractionValidator _validator = new();

    [Fact]
    public void Validate_ValidResult_ReturnsNoErrors()
    {
        var result = new DocumentExtractionResult(
            "Prescription",
            [
                new ExtractedItemResult(
                    "Medication",
                    1,
                    1,
                    [
                        new ExtractedFieldResult(
                            "MedicationName",
                            "Example",
                            0.9m,
                            "Example",
                            [])
                    ])
            ]);

        var validation = _validator.Validate(result);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }

    [Fact]
    public void Validate_InvalidResult_ReturnsAllDetectedErrors()
    {
        var result = new DocumentExtractionResult(
            "UnsupportedDocument",
            [
                new ExtractedItemResult(
                    "UnsupportedItem",
                    0,
                    0,
                    [
                        new ExtractedFieldResult(
                            "UnsupportedField",
                            null,
                            2m,
                            null,
                            ["UnsupportedIssue"])
                    ])
            ]);

        var validation = _validator.Validate(result);

        Assert.False(validation.IsValid);
        Assert.True(validation.Errors.Count >= 7);
    }
}
