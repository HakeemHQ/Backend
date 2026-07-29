using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Features.MedicalDocuments.Commands.UploadDocument;

public sealed class UploadDocumentCommandHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalDocumentRepository medicalDocumentRepository,
    IOutboxEventRepository outboxEventRepository,
    IDocumentFileStorage documentFileStorage,
    IUnitOfWork unitOfWork,
    ILogger<UploadDocumentCommandHandler> logger)
    : IRequestHandler<UploadDocumentCommand, UploadDocumentResult>
{
    public async Task<UploadDocumentResult> Handle(
        UploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var patientProfile = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patientProfile is null)
        {
            throw new UnAuthorizedException(ErrorCodes.DocumentPatientProfileNotFound);
        }

        var documentId = Guid.NewGuid();
        var filePath = await documentFileStorage.SaveAsync(
            request.File!,
            documentId,
            cancellationToken);

        var medicalDocument = new MedicalDocument
        {
            Id = documentId,
            PatientProfileId = patientProfile.Id,
            DocumentType = request.DocumentType.Trim(),
            Title = request.Title.Trim(),
            DocumentDate = request.DocumentDate.ToDateTime(TimeOnly.MinValue),
            FilePath = filePath
        };
        medicalDocument.QueueExtraction();

        medicalDocumentRepository.Add(medicalDocument);
        outboxEventRepository.Add(
            new DocumentExtractionRequestedEvent
            {
                DocumentId = documentId
            },
            $"document-extraction:{documentId}");

        try
        {
            // EF Core wraps all changes in this SaveChanges call in one SQL transaction.
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

        return new UploadDocumentResult(
            medicalDocument.Id,
            medicalDocument.DocumentType,
            medicalDocument.Title,
            DateOnly.FromDateTime(medicalDocument.DocumentDate),
            medicalDocument.ExtractionStatus.ToString());
    }
}
