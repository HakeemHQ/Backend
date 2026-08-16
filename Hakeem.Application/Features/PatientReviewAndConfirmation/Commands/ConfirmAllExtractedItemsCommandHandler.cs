using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.PatientReviewAndConfirmation.DTOs;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Services.Access;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Enums.Reviews;
using MediatR;

namespace Hakeem.Application.Features.PatientReviewAndConfirmation.Commands;

public sealed class ConfirmAllExtractedItemsCommandHandler(
    IDoctorPatientAccessGuard doctorPatientAccessGuard,
    IMedicalDocumentRepository medicalDocumentRepository,
    IRequestHandler<PatientReviewConfirmationCommand, ReviewExtractedItemResult>
        itemConfirmationHandler)
    : IRequestHandler<ConfirmAllExtractedItemsCommand, ConfirmAllExtractedItemsResult>
{
    public async Task<ConfirmAllExtractedItemsResult> Handle(
        ConfirmAllExtractedItemsCommand request,
        CancellationToken cancellationToken)
    {
        var document = await medicalDocumentRepository.GetByIdAsync(
            request.DocumentId,
            cancellationToken);

        if (document is null)
        {
            throw new NotFoundException(ErrorCodes.DocumentNotFound);
        }

        await doctorPatientAccessGuard.RequireDoctorWithActiveAccessAsync(
            document.PatientProfileId,
            cancellationToken);

        if (document.ExtractionStatus != ExtractionStatus.Completed)
        {
            throw new ConflictException(ErrorCodes.ExtractionNotCompleted);
        }

        var extractedItems = await medicalDocumentRepository.GetExtractedItemsAsync(
            document.Id,
            cancellationToken);
        var itemsToConfirm = extractedItems
            .Where(item => item.ReviewStatus != ExtractedItemReviewStatus.Reviewed)
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
            extractedItems.Count == 0
                ? DocumentReviewStatus.NotReviewed.ToString()
                : DocumentReviewStatus.FullyReviewed.ToString(),
            results.Count,
            extractedItems.Count - itemsToConfirm.Count,
            results);
    }
}
