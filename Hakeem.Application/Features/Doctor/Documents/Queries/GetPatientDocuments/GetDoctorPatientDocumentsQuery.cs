using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using MediatR;

namespace Hakeem.Application.Features.Doctor.Documents.Queries.GetPatientDocuments;

public sealed record GetDoctorPatientDocumentsQuery(
    Guid PatientProfileId,
    string? DocumentName,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PaginatedResult<MedicalDocumentDto>>;
