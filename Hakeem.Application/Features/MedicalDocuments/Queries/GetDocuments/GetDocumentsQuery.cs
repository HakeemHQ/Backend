using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using MediatR;

namespace Hakeem.Application.Features.MedicalDocuments.Queries.GetDocuments;

public sealed record GetDocumentsQuery(
    string? DocumentName,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PaginatedResult<MedicalDocumentDto>>;
