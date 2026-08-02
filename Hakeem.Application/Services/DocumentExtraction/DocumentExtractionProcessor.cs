using System.Text.Json;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Application.Interfaces.Validation;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Services.DocumentExtraction;

public sealed class DocumentExtractionProcessor(
    IMedicalDocumentRepository medicalDocumentRepository,
    IDocumentProcessingAgent documentProcessingAgent,
    IDocumentExtractionValidator extractionValidator,
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

        var validationResult = extractionValidator.Validate(
            extractionResult);

        if (!validationResult.IsValid)
        {
            throw new InvalidDataException(
                string.Join(Environment.NewLine, validationResult.Errors));
        }

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
                            FieldName = DocumentExtractionSchema.CanonicalizeFieldName(
                                item.ItemType,
                                field.FieldName),
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
