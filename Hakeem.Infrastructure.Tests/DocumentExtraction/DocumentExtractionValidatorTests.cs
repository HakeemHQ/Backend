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
            MedicalDocumentType.Prescription,
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
            MedicalDocumentType.Other,
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
        Assert.True(validation.Errors.Count >= 6);
    }

    [Fact]
    public void Validate_MultipleLabRowsAsSeparateItems_ReturnsNoErrors()
    {
        var result = new DocumentExtractionResult(
            MedicalDocumentType.LabReport,
            [
                CreateLabResult(1, "Cholesterol", "198", "mg/dL"),
                CreateLabResult(2, "LDL Cholesterol", "135", "mg/dL")
            ]);

        var validation = _validator.Validate(result);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }

    private static ExtractedItemResult CreateLabResult(
        int sequenceNumber,
        string testName,
        string value,
        string unit)
    {
        return new ExtractedItemResult(
            "LabResult",
            sequenceNumber,
            1,
            [
                new ExtractedFieldResult(
                    "LabTestName",
                    testName,
                    0.99m,
                    testName,
                    []),
                new ExtractedFieldResult(
                    "LabValue",
                    value,
                    0.99m,
                    value,
                    []),
                new ExtractedFieldResult(
                    "Unit",
                    unit,
                    0.99m,
                    unit,
                    [])
            ]);
    }
}
