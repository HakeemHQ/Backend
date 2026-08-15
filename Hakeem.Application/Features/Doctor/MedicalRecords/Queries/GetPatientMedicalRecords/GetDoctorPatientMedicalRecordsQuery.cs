using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using MediatR;

namespace Hakeem.Application.Features.Doctor.MedicalRecords.Queries.GetPatientMedicalRecords;

public sealed record GetDoctorPatientMedicalRecordsQuery(
    Guid PatientProfileId,
    string? Search,
    string? RecordType,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PaginatedResult<MedicalRecordDto>>;
