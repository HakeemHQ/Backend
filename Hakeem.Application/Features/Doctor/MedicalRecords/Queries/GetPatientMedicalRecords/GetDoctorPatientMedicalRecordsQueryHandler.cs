using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Services.Access;
using MediatR;

namespace Hakeem.Application.Features.Doctor.MedicalRecords.Queries.GetPatientMedicalRecords;

public sealed class GetDoctorPatientMedicalRecordsQueryHandler(
    IDoctorPatientAccessGuard doctorPatientAccessGuard,
    IMedicalRecordsRepository medicalRecordRepository)
    : IRequestHandler<GetDoctorPatientMedicalRecordsQuery, PaginatedResult<MedicalRecordDto>>
{
    public async Task<PaginatedResult<MedicalRecordDto>> Handle(
        GetDoctorPatientMedicalRecordsQuery request,
        CancellationToken cancellationToken)
    {
        await doctorPatientAccessGuard.RequireDoctorWithActiveAccessAsync(
            request.PatientProfileId,
            cancellationToken);

        var pagedRecords = await medicalRecordRepository.GetMedicalRecordsAsync(
            request.PatientProfileId,
            request.Search,
            request.RecordType,
            request.FromDate,
            request.ToDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = pagedRecords.Items.Select(record => new MedicalRecordDto
        {
            MedicalRecordId = record.Id,
            RecordType = record.RecordType,
            DisplayName = record.DisplayName,
            ClinicalDate = record.ClinicalDate,
            Status = record.Status
        });

        return new PaginatedResult<MedicalRecordDto>(
            items,
            pagedRecords.TotalCount,
            pagedRecords.PageNumber,
            pagedRecords.PageSize);
    }
}
