using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Application.Repositories.PatientReviewConfirmation;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Enums.Reviews;
using Hakeem.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Hakeem.Application.Features.PatientReviewAndConfirmation.Commands
{
    public sealed class PatientReviewConfirmationCommandHandler(ICurrentUserContext currentUserContext,
     IPatientProfileRepository patientProfileRepository,IMedicalDocumentRepository medicalDocumentRepository,
     IMedicalRecordsRepository medicalRecordRepository,IFieldReviewRepository fieldReviewRepository,
     ISourceReferenceRepository sourceReferenceRepository,IUnitOfWork unitOfWork)
     :IRequestHandler< PatientReviewConfirmationCommand,ReviewExtractedItemResult>
    {
        public async Task<ReviewExtractedItemResult> Handle(PatientReviewConfirmationCommand request,CancellationToken cancellationToken)
        {
            // Get current patient
            var patient = await patientProfileRepository.GetByUserIdAsync(currentUserContext.UserId,cancellationToken);

            if (patient is null)
                throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);

            // Load ExtractedItem
            var item = await medicalDocumentRepository.GetExtractedItemForReviewAsync(request.ExtractedItemId,cancellationToken);

            if (item is null)
                throw new NotFoundException(ErrorCodes.ExtractedItemNotFound);

            if (item.MedicalDocument.PatientProfileId != patient.Id)
                throw new NotFoundException(ErrorCodes.ExtractedItemNotFound);

            if (item.MedicalDocument.ExtractionStatus != ExtractionStatus.Completed)
                throw new ConflictException(ErrorCodes.ExtractionNotCompleted);

            // Validation
            var duplicateIds = request.Fields.GroupBy(x => x.ExtractedFieldId).Where(x => x.Count() > 1).Select(x => x.Key);

            if (duplicateIds.Any())
                throw new ValidationException("Duplicate field ids are not allowed.");

            var extractedFields = item.ExtractedFields.ToDictionary(x => x.Id);

            foreach (var field in request.Fields)
            {
                if (!extractedFields.TryGetValue(field.ExtractedFieldId, out _))
                {
                    throw new ValidationException($"Field '{field.ExtractedFieldId}' does not belong to this ExtractedItem.");
                }

                if (field.Decision == FieldReviewDecision.Corrected && string.IsNullOrWhiteSpace(field.CorrectedValue))
                {
                    throw new ValidationException($"CorrectedValue is required for field '{field.ExtractedFieldId}'.");
                }
            }
        
            // Save Reviews
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
                review.CorrectedValue = requestField.Decision == FieldReviewDecision.Corrected ?
                                        requestField.CorrectedValue: null;

                review.ReviewedAt = DateTime.UtcNow;
            }   
            
            //Update Extracted Item
            item.ReviewStatus = ExtractedItemReviewStatus.Confirmed;
            item.ReviewedAt = DateTimeOffset.UtcNow;

            //Create Medical Record
            var displayName = string.Join(", ",item.ExtractedFields.Select(f => $"{f.FieldName}: {f.ExtractedValue}"));
            var medicalRecord = new MedicalRecord
            {
                Id = Guid.NewGuid(),
                RecordType = item.ItemType,
                SourceExtractedItemId = item.Id,
                PatientProfileId = patient.Id,
                DisplayName = displayName,
                Status = "Confirmed",
                ClinicalDate = item.CreatedAt,
            };
            medicalRecordRepository.Add(medicalRecord);
     
            // Create MedicalRecordFields
            foreach (var extractedField in item.ExtractedFields)
            {
                var decision = extractedField.FieldReview?.Decision ?? FieldReviewDecision.Approved;
                if (decision == FieldReviewDecision.Rejected)
                    continue;
                var value = decision == FieldReviewDecision.Corrected ? extractedField.FieldReview!.CorrectedValue!
                                         : extractedField.ExtractedValue;

                medicalRecord.Fields.Add(new MedicalRecordField
                {
                    Id = Guid.NewGuid(),
                    MedicalRecordId = medicalRecord.Id,
                    FieldName = extractedField.FieldName,
                    SourceExtractedFieldId = extractedField.Id,
                    Value = value
                });
            }

            // Create Source Reference
            sourceReferenceRepository.Add(new SourceReference
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = medicalRecord.Id,
                DocumentId = item.MedicalDocumentId,
                PageReference = item.PageNumber.ToString(),
            });

            // Save
            await unitOfWork.SaveChanges(cancellationToken);
            return new ReviewExtractedItemResult(item.Id,item.ItemType.ToString(), item.ReviewStatus.ToString(),
                                                  medicalRecord.Id,item.ReviewedAt!.Value,
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
