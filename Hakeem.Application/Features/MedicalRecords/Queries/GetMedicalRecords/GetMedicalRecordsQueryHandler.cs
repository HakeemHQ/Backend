using Hakeem.Application.Common;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Application.Repositories.PatientReviewConfirmation;
using MediatR;
namespace Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecords
{
    public sealed class GetMedicalRecordsQueryHandler(ICurrentUserContext currentUserContext,
                        IPatientProfileRepository patientProfileRepository,IMedicalRecordsRepository medicalRecordRepository)
     : IRequestHandler<GetMedicalRecordsQuery, PaginatedResult<MedicalRecordDto>>
    {
        public async Task<PaginatedResult<MedicalRecordDto>> Handle(GetMedicalRecordsQuery request,CancellationToken cancellationToken)
        {
            // Get current patient
            var patient = await patientProfileRepository.GetByUserIdAsync(currentUserContext.UserId,cancellationToken);

            if (patient is null)
                throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);

            // Get paged medical records
            var pagedRecords = await medicalRecordRepository.GetMedicalRecordsAsync(patient.Id,request.Search,
                               request.RecordType,request.FromDate,request.ToDate,request.PageNumber,request.PageSize,
                               cancellationToken);

            // Map entity => DTO
            var items = pagedRecords.Items.Select(record => new MedicalRecordDto
            {
                MedicalRecordId = record.Id,
                RecordType = record.RecordType,
                DisplayName = record.DisplayName,
                ClinicalDate = record.ClinicalDate,
                Status = record.Status
            });

            // Return paginated result
            return new PaginatedResult<MedicalRecordDto>(items,pagedRecords.TotalCount,pagedRecords.PageNumber,pagedRecords.PageSize);
        }
    }
}
