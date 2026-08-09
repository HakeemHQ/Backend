using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.PatientReviewAndConfirmation.DTOs;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Enums.Reviews;
using MediatR;

namespace Hakeem.Application.Features.PatientReviewAndConfirmation.Commands;

public sealed class ConfirmAllExtractedItemsCommandHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalDocumentRepository medicalDocumentRepository,
    IRequestHandler<PatientReviewConfirmationCommand, ReviewExtractedItemResult>
        itemConfirmationHandler)
    : IRequestHandler<ConfirmAllExtractedItemsCommand, ConfirmAllExtractedItemsResult>
{
    public async Task<ConfirmAllExtractedItemsResult> Handle(
        ConfirmAllExtractedItemsCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var document = await medicalDocumentRepository.GetByIdAsync(
            request.DocumentId,
            cancellationToken);

        if (document is null || document.PatientProfileId != patient.Id)
        {
            throw new NotFoundException(ErrorCodes.DocumentNotFound);
        }

        if (document.ExtractionStatus != ExtractionStatus.Completed)
        {
            throw new ConflictException(ErrorCodes.ExtractionNotCompleted);
        }

        var extractedItems = await medicalDocumentRepository.GetExtractedItemsAsync(
            document.Id,
            cancellationToken);
        var itemsToConfirm = extractedItems
            .Where(item => item.ReviewStatus != ExtractedItemReviewStatus.Confirmed)
            .OrderBy(item => item.SequenceNumber)
            .ThenBy(item => item.Id)
            .ToList();
        var results = new List<ReviewExtractedItemResult>(itemsToConfirm.Count);

        foreach (var item in itemsToConfirm)
        {
            var confirmation = new PatientReviewConfirmationCommand
            {
                ExtractedItemId = item.Id,
                Fields = item.ExtractedFields
                    .Select(field => new PatientReviewConfirmDTO
                    {
                        ExtractedFieldId = field.Id,
                        Decision = FieldReviewDecision.Approved
                    })
                    .ToList()
            };

            results.Add(await itemConfirmationHandler.Handle(
                confirmation,
                cancellationToken));
        }

        return new ConfirmAllExtractedItemsResult(
            document.Id,
            results.Count,
            extractedItems.Count - itemsToConfirm.Count,
            results);
    }
}
