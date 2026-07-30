using System.Text.Json;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Services.DocumentExtraction;

public sealed class DocumentExtractionProcessor(
    IMedicalDocumentRepository medicalDocumentRepository,
    IDocumentProcessingAgent documentProcessingAgent,
    IUnitOfWork unitOfWork,
    ILogger<DocumentExtractionProcessor> logger)
    : IDocumentExtractionProcessor
{
    public async Task ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "A document ID is required.",
                nameof(documentId));
        }

        var medicalDocument =
            await medicalDocumentRepository.GetByIdAsync(
                documentId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Medical document '{documentId}' was not found.");

        if (medicalDocument.ExtractionStatus ==
            ExtractionStatus.Completed)
        {
            logger.LogInformation(
                "Skipping completed extraction for document {DocumentId}.",
                documentId);
            return;
        }

        if (medicalDocument.ExtractionStatus ==
            ExtractionStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Medical document '{documentId}' is in the Failed extraction state.");
        }

        if (medicalDocument.ExtractionStatus ==
            ExtractionStatus.Queued)
        {
            medicalDocument.StartExtraction();
            await unitOfWork.SaveChanges(cancellationToken);
        }

        var extractionResult =
            await documentProcessingAgent.ProcessAsync(
                documentId,
                cancellationToken);

        ValidateEntireResult(extractionResult);

        var extractedItems = MapExtractionResult(
            medicalDocument.Id,
            extractionResult);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingItems =
                await medicalDocumentRepository
                    .GetExtractedItemsAsync(
                        medicalDocument.Id,
                        cancellationToken);

            if (existingItems.Count > 0)
            {
                medicalDocumentRepository.RemoveExtractedItems(
                    existingItems);

                // Execute replacement deletes first to avoid unique-key
                // conflicts. The surrounding transaction keeps this safe.
                await unitOfWork.SaveChanges(cancellationToken);
            }

            medicalDocumentRepository.AddExtractedItems(
                extractedItems);

            medicalDocument.DocumentType =
                extractionResult.DocumentType;
            medicalDocument.CompleteExtraction();

            await unitOfWork.SaveChanges(cancellationToken);
            await unitOfWork.CommitTransactionAsync();

            logger.LogInformation(
                "Persisted {ItemCount} extracted items for document {DocumentId}.",
                extractedItems.Count,
                documentId);
        }
        catch
        {
            await unitOfWork.RollBackTransactionAsync();
            throw;
        }
    }

    private static void ValidateEntireResult(
        DocumentExtractionResult? result)
    {
        if (result is null)
        {
            throw new InvalidDataException(
                "The document extraction result cannot be null.");
        }

        if (!DocumentExtractionSchema.DocumentTypes.Contains(
                result.DocumentType))
        {
            throw new InvalidDataException(
                $"Document type '{result.DocumentType}' is not allowed.");
        }

        if (result.Items is null)
        {
            throw new InvalidDataException(
                "The extraction items array cannot be null.");
        }

        var itemIdentities = new HashSet<(string, int)>();

        foreach (var item in result.Items)
        {
            if (item is null)
            {
                throw new InvalidDataException(
                    "The extraction result contains a null item.");
            }

            if (!DocumentExtractionSchema.ItemTypes.Contains(
                    item.ItemType))
            {
                throw new InvalidDataException(
                    $"Item type '{item.ItemType}' is not allowed.");
            }

            if (item.SequenceNumber <= 0)
            {
                throw new InvalidDataException(
                    "Item sequence numbers must be positive.");
            }

            if (item.PageNumber <= 0)
            {
                throw new InvalidDataException(
                    "Item page numbers must be positive.");
            }

            if (!itemIdentities.Add(
                    (item.ItemType, item.SequenceNumber)))
            {
                throw new InvalidDataException(
                    $"Duplicate item '{item.ItemType}' sequence '{item.SequenceNumber}'.");
            }

            if (item.Fields is null)
            {
                throw new InvalidDataException(
                    "The extraction fields array cannot be null.");
            }

            var fieldNames = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (var field in item.Fields)
            {
                ValidateField(field, fieldNames);
            }
        }
    }

    private static void ValidateField(
        ExtractedFieldResult? field,
        ISet<string> fieldNames)
    {
        if (field is null)
        {
            throw new InvalidDataException(
                "The extraction result contains a null field.");
        }

        if (!DocumentExtractionSchema.FieldNames.Contains(
                field.FieldName))
        {
            throw new InvalidDataException(
                $"Field name '{field.FieldName}' is not allowed.");
        }

        if (!fieldNames.Add(field.FieldName))
        {
            throw new InvalidDataException(
                $"Duplicate field '{field.FieldName}' in one item.");
        }

        if (field.Confidence is < 0 or > 1)
        {
            throw new InvalidDataException(
                $"Field '{field.FieldName}' has an invalid confidence.");
        }

        if (field.Issues is null)
        {
            throw new InvalidDataException(
                $"Field '{field.FieldName}' issues cannot be null.");
        }

        foreach (var issue in field.Issues)
        {
            if (issue is null ||
                !DocumentExtractionSchema.Issues.Contains(issue))
            {
                throw new InvalidDataException(
                    $"Field '{field.FieldName}' contains an invalid issue.");
            }
        }
    }

    private static IReadOnlyList<ExtractedItem>
        MapExtractionResult(
            Guid medicalDocumentId,
            DocumentExtractionResult result)
    {
        return result.Items
            .Select(item =>
            {
                var extractedItem = new ExtractedItem
                {
                    Id = Guid.NewGuid(),
                    MedicalDocumentId = medicalDocumentId,
                    ItemType = item.ItemType,
                    SequenceNumber = item.SequenceNumber,
                    PageNumber = item.PageNumber
                };

                foreach (var field in item.Fields)
                {
                    extractedItem.ExtractedFields.Add(
                        new ExtractedField
                        {
                            Id = Guid.NewGuid(),
                            ExtractedItemId = extractedItem.Id,
                            FieldName = field.FieldName,
                            ExtractedValue = field.Value,
                            Confidence = field.Confidence,
                            EvidenceText = field.EvidenceText,
                            Issues = JsonSerializer.Serialize(
                                field.Issues)
                        });
                }

                return extractedItem;
            })
            .ToList();
    }
}
