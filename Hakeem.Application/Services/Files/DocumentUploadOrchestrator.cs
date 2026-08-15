using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Repositories.AuditLogs;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Services.Files;

public sealed record DocumentUploadResult(
    Guid DocumentId,
    string DocumentType,
    string Title,
    DateOnly DocumentDate,
    string ExtractionStatus);

public interface IDocumentUploadOrchestrator : IScoped
{
    Task<DocumentUploadResult> UploadAsync(
        Guid patientProfileId,
        Guid actorUserId,
        IFormFile file,
        string title,
        DateOnly documentDate,
        CancellationToken cancellationToken);
}

public sealed class DocumentUploadOrchestrator(
    IMedicalDocumentRepository medicalDocumentRepository,
    IAuditLogRepository auditLogRepository,
    IOutboxEventRepository outboxEventRepository,
    IDocumentFileStorage documentFileStorage,
    IUnitOfWork unitOfWork,
    ILogger<DocumentUploadOrchestrator> logger)
    : IDocumentUploadOrchestrator
{
    public async Task<DocumentUploadResult> UploadAsync(
        Guid patientProfileId,
        Guid actorUserId,
        IFormFile file,
        string title,
        DateOnly documentDate,
        CancellationToken cancellationToken)
    {
        var documentId = Guid.NewGuid();
        var filePath = await documentFileStorage.SaveAsync(
            file,
            documentId,
            cancellationToken);

        var medicalDocument = new MedicalDocument
        {
            Id = documentId,
            PatientProfileId = patientProfileId,
            DocumentType = MedicalDocument.UnclassifiedDocumentType,
            Title = title.Trim(),
            DocumentDate = documentDate.ToDateTime(TimeOnly.MinValue),
            FilePath = filePath
        };
        medicalDocument.QueueExtraction();

        medicalDocumentRepository.Add(medicalDocument);

        auditLogRepository.Add(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                PatientProfileId = patientProfileId,
                Action = "DocumentUploaded",
                Target = $"MedicalDocument:{documentId}",
                OccurredAt = DateTime.UtcNow
            });

        outboxEventRepository.Add(
            new DocumentExtractionRequestedEvent
            {
                DocumentId = documentId
            },
            $"document-extraction:{documentId}");

        try
        {
            await unitOfWork.SaveChanges(cancellationToken);
        }
        catch
        {
            try
            {
                await documentFileStorage.DeleteAsync(filePath, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(
                    cleanupException,
                    "Failed to delete orphaned document file {FilePath}.",
                    filePath);
            }

            throw;
        }

        return new DocumentUploadResult(
            medicalDocument.Id,
            medicalDocument.DocumentType,
            medicalDocument.Title,
            DateOnly.FromDateTime(medicalDocument.DocumentDate),
            medicalDocument.ExtractionStatus.ToString());
    }
}
