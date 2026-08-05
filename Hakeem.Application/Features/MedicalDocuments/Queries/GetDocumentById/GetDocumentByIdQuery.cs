using Hakeem.Application.Features.MedicalDocuments.DTOs;
using MediatR;

namespace Hakeem.Application.Features.MedicalDocuments.Queries.GetDocumentById;

public sealed record GetDocumentByIdQuery(Guid DocumentId)
    : IRequest<GetDocumentByIdResult>;
