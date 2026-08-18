using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using MediatR;

namespace Hakeem.Application.Features.Doctor.MedicalRecords.Queries.GetMedicalRecordsByDocument;

public sealed record GetMedicalRecordsByDocumentQuery(
    Guid DocumentId,
    string? Search,
    string? RecordType,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PaginatedResult<MedicalRecordDto>>;
