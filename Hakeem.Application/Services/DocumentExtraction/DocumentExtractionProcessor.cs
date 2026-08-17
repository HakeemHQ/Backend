using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Constants;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Application.Interfaces.Validation;
using Hakeem.Application.Repositories.AuditLogs;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hakeem.Application.Services.DocumentExtraction;

public sealed class DocumentExtractionProcessor(
    IMedicalDocumentRepository medicalDocumentRepository,
    IDocumentProcessingAgent documentProcessingAgent,
    IDocumentExtractionValidator extractionValidator,
    IAuditLogRepository auditLogRepository,
    IDocumentFileStorage documentFileStorage,
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

        if (medicalDocument.ExtractionStatus == ExtractionStatus.Rejected)
        {
            await CleanupRejectedFileAsync(
                medicalDocument,
                cancellationToken);
            return;
        }

        if (medicalDocument.ExtractionStatus ==
            ExtractionStatus.Queued)
        {
            medicalDocument.StartExtraction();
            auditLogRepository.Add(
    new AuditLog
    {
        Id = Guid.NewGuid(),
        ActorUserId = null, // extraction is background process
        PatientProfileId = medicalDocument.PatientProfileId,
        Action = "DocumentExtractionStarted",
        Target = $"MedicalDocument:{documentId}",
        OccurredAt = DateTime.UtcNow
    });
            await unitOfWork.SaveChanges(cancellationToken);
        }

        var processingResult =
            await documentProcessingAgent.ProcessAsync(
                documentId,
                cancellationToken);

        if (!processingResult.Classification.IsMedical)
        {
            medicalDocument.RejectExtraction(ErrorCodes.DocumentNotMedical);
            auditLogRepository.Add(
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = null,
                    PatientProfileId = medicalDocument.PatientProfileId,
                    Action = "DocumentRejectedAsNonMedical",
                    Target = $"MedicalDocument:{documentId}",
                    OccurredAt = DateTime.UtcNow
                });

            // Persist the terminal outcome before removing the original binary.
            await unitOfWork.SaveChanges(cancellationToken);
            await CleanupRejectedFileAsync(
                medicalDocument,
                cancellationToken);
            return;
        }

        var extractionResult = processingResult.Extraction
            ?? throw new InvalidDataException(
                "A medical document processing result must include an extraction.");

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

            medicalDocument.DocumentType =extractionResult.DocumentType.ToString();
            medicalDocument.CompleteExtraction();
            auditLogRepository.Add(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = null,
                PatientProfileId = medicalDocument.PatientProfileId,
                Action = "DocumentExtractionCompleted",
                Target = $"MedicalDocument:{documentId}",
                OccurredAt = DateTime.UtcNow
            });
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

    private async Task CleanupRejectedFileAsync(
        MedicalDocument medicalDocument,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(medicalDocument.FilePath))
        {
            return;
        }

        var filePath = medicalDocument.FilePath;
        await documentFileStorage.DeleteAsync(
            filePath,
            cancellationToken);

        medicalDocument.FilePath = string.Empty;
        await unitOfWork.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Deleted rejected document file {FilePath} for document {DocumentId}.",
            filePath,
            medicalDocument.Id);
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
