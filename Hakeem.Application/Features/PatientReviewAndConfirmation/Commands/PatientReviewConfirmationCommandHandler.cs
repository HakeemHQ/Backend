using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.PatientReviewConfirmation;
using Hakeem.Application.Services.Access;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Enums.Reviews;
using Hakeem.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Hakeem.Application.Features.PatientReviewAndConfirmation.Commands
{
    public sealed class PatientReviewConfirmationCommandHandler(
        ICurrentUserContext currentUserContext,
        IDoctorPatientAccessGuard doctorPatientAccessGuard,
        IMedicalDocumentRepository medicalDocumentRepository,
        IMedicalRecordsRepository medicalRecordRepository,
        IFieldReviewRepository fieldReviewRepository,
        ISourceReferenceRepository sourceReferenceRepository,
        IMedicalRecordIndexOutbox medicalRecordIndexOutbox,
        IUnitOfWork unitOfWork)
        : IRequestHandler<PatientReviewConfirmationCommand, ReviewExtractedItemResult>
    {
        public async Task<ReviewExtractedItemResult> Handle(
            PatientReviewConfirmationCommand request,
            CancellationToken cancellationToken)
        {
            var item = await medicalDocumentRepository.GetExtractedItemForReviewAsync(
                request.ExtractedItemId,
                cancellationToken);

            if (item is null)
            {
                throw new NotFoundException(ErrorCodes.ExtractedItemNotFound);
            }

            await doctorPatientAccessGuard.RequireDoctorWithActiveAccessAsync(
                item.MedicalDocument.PatientProfileId,
                cancellationToken);

            if (item.MedicalDocument.ExtractionStatus != ExtractionStatus.Completed)
            {
                throw new ConflictException(ErrorCodes.ExtractionNotCompleted);
            }

            var duplicateIds = request.Fields
                .GroupBy(x => x.ExtractedFieldId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);

            if (duplicateIds.Any())
            {
                throw new ValidationException("Duplicate field ids are not allowed.");
            }

            var extractedFields = item.ExtractedFields.ToDictionary(x => x.Id);

            foreach (var field in request.Fields)
            {
                if (!extractedFields.TryGetValue(field.ExtractedFieldId, out _))
                {
                    throw new ValidationException(
                        $"Field '{field.ExtractedFieldId}' does not belong to this ExtractedItem.");
                }

                if (field.Decision == FieldReviewDecision.Corrected &&
                    string.IsNullOrWhiteSpace(field.CorrectedValue))
                {
                    throw new ValidationException(
                        $"CorrectedValue is required for field '{field.ExtractedFieldId}'.");
                }
            }

            foreach (var requestField in request.Fields)
            {
                var extractedField = extractedFields[requestField.ExtractedFieldId];

                var review = extractedField.FieldReview;

                if (review is null)
                {
                    review = new FieldReview
                    {
                        Id = Guid.NewGuid(),
                        ExtractedFieldId = extractedField.Id
                    };

                    fieldReviewRepository.Add(review);
                    extractedField.FieldReview = review;
                }

                review.Decision = requestField.Decision;
                review.CorrectedValue = requestField.Decision == FieldReviewDecision.Corrected
                    ? requestField.CorrectedValue
                    : null;
                review.ReviewedAt = DateTime.UtcNow;
            }

            item.ReviewStatus = ExtractedItemReviewStatus.Reviewed;
            item.ReviewedAt = DateTimeOffset.UtcNow;
            item.ReviewedByUserId = currentUserContext.UserId;
            item.MedicalDocument.RefreshReviewStatus();

            var patientProfileId = item.MedicalDocument.PatientProfileId;

            var acceptedFields = item.ExtractedFields
                .Where(f =>
                    (f.FieldReview?.Decision ?? FieldReviewDecision.Approved) !=
                    FieldReviewDecision.Rejected)
                .ToList();

            Guid? medicalRecordId = null;

            if (acceptedFields.Count > 0)
            {
                var displayName = string.Join(
                    ", ",
                    acceptedFields.Select(f =>
                    {
                        var value = (f.FieldReview?.Decision ?? FieldReviewDecision.Approved) ==
                            FieldReviewDecision.Corrected
                            ? f.FieldReview!.CorrectedValue!
                            : f.ExtractedValue;

                        return $"{f.FieldName}: {value}";
                    }));

                var medicalRecord = new MedicalRecord
                {
                    Id = Guid.NewGuid(),
                    RecordType = item.ItemType,
                    SourceExtractedItemId = item.Id,
                    PatientProfileId = patientProfileId,
                    DisplayName = displayName,
                    Status = "Confirmed",
                    ClinicalDate = item.CreatedAt,
                };
                medicalRecordRepository.Add(medicalRecord);

                foreach (var extractedField in acceptedFields)
                {
                    var decision = extractedField.FieldReview?.Decision ?? FieldReviewDecision.Approved;
                    var value = decision == FieldReviewDecision.Corrected
                        ? extractedField.FieldReview!.CorrectedValue!
                        : extractedField.ExtractedValue;

                    var field = new MedicalRecordField
                    {
                        Id = Guid.NewGuid(),
                        MedicalRecordId = medicalRecord.Id,
                        FieldName = extractedField.FieldName,
                        SourceExtractedFieldId = extractedField.Id,
                        Value = value ?? string.Empty
                    };

                    medicalRecord.Fields.Add(field);
                }

                medicalRecordIndexOutbox.EnqueueIndexing(medicalRecord.Id);

                sourceReferenceRepository.Add(new SourceReference
                {
                    Id = Guid.NewGuid(),
                    MedicalRecordId = medicalRecord.Id,
                    DocumentId = item.MedicalDocumentId,
                    PageReference = item.PageNumber.ToString(),
                });

                medicalRecordId = medicalRecord.Id;
            }

            await unitOfWork.SaveChanges(cancellationToken);

            return new ReviewExtractedItemResult(
                item.Id,
                item.ItemType.ToString(),
                item.ReviewStatus.ToString(),
                item.MedicalDocument.ReviewStatus.ToString(),
                medicalRecordId,
                item.ReviewedAt!.Value,
                item.ExtractedFields.Select(field =>
                {
                    var review = field.FieldReview;
                    var decision = review?.Decision ?? FieldReviewDecision.Approved;
                    return new ReviewedFieldResult(
                        field.Id,
                        field.FieldName,
                        field.ExtractedValue,
                        decision,
                        review?.CorrectedValue,
                        decision switch
                        {
                            FieldReviewDecision.Approved => field.ExtractedValue,
                            FieldReviewDecision.Corrected => review?.CorrectedValue,
                            FieldReviewDecision.Rejected => null,
                            _ => field.ExtractedValue
                        });
                }).ToList());
        }
    }
}
