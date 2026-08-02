using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Validation;

namespace Hakeem.Application.Services.DocumentExtraction;

public sealed class DocumentExtractionValidator
    : IDocumentExtractionValidator
{
    public DocumentExtractionValidationResult Validate(
        DocumentExtractionResult? result)
    {
        var errors = new List<string>();

        if (result is null)
        {
            errors.Add("The document extraction result cannot be null.");
            return new DocumentExtractionValidationResult(errors);
        }

        if (!DocumentExtractionSchema.DocumentTypes.Contains(
                result.DocumentType))
        {
            errors.Add(
                $"Document type '{result.DocumentType}' is not allowed.");
        }

        if (result.Items is null)
        {
            errors.Add("The extraction items array cannot be null.");
            return new DocumentExtractionValidationResult(errors);
        }

        var itemIdentities = new HashSet<(string, int)>();

        foreach (var item in result.Items)
        {
            if (item is null)
            {
                errors.Add("The extraction result contains a null item.");
                continue;
            }

            if (!DocumentExtractionSchema.ItemTypes.Contains(
                    item.ItemType))
            {
                errors.Add(
                    $"Item type '{item.ItemType}' is not allowed.");
            }

            if (item.SequenceNumber <= 0)
            {
                errors.Add("Item sequence numbers must be positive.");
            }

            if (item.PageNumber <= 0)
            {
                errors.Add("Item page numbers must be positive.");
            }

            if (!itemIdentities.Add(
                    (item.ItemType, item.SequenceNumber)))
            {
                errors.Add(
                    $"Duplicate item '{item.ItemType}' sequence '{item.SequenceNumber}'.");
            }

            if (item.Fields is null)
            {
                errors.Add("The extraction fields array cannot be null.");
                continue;
            }

            var fieldNames = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (var field in item.Fields)
            {
                ValidateField(
                    field,
                    item.ItemType,
                    fieldNames,
                    errors);
            }
        }

        return new DocumentExtractionValidationResult(errors);
    }

    private static void ValidateField(
        ExtractedFieldResult? field,
        string itemType,
        ISet<string> fieldNames,
        ICollection<string> errors)
    {
        if (field is null)
        {
            errors.Add("The extraction result contains a null field.");
            return;
        }

        var fieldName = DocumentExtractionSchema.CanonicalizeFieldName(
            itemType,
            field.FieldName);

        if (!DocumentExtractionSchema.FieldNames.Contains(fieldName))
        {
            errors.Add(
                $"Field name '{field.FieldName}' is not allowed.");
        }

        if (!fieldNames.Add(fieldName))
        {
            errors.Add(
                $"Duplicate field '{fieldName}' in one item.");
        }

        if (field.Confidence is < 0 or > 1)
        {
            errors.Add(
                $"Field '{field.FieldName}' has an invalid confidence.");
        }

        if (field.Issues is null)
        {
            errors.Add(
                $"Field '{field.FieldName}' issues cannot be null.");
            return;
        }

        foreach (var issue in field.Issues)
        {
            if (issue is null ||
                !DocumentExtractionSchema.Issues.Contains(issue))
            {
                errors.Add(
                    $"Field '{field.FieldName}' contains an invalid issue.");
            }
        }
    }
}
