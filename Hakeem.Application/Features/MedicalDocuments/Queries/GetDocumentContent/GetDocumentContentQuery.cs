using MediatR;

namespace Hakeem.Application.Features.MedicalDocuments.Queries.GetDocumentContent;

public sealed record GetDocumentContentQuery(Guid DocumentId)
    : IRequest<GetDocumentContentResult>;

public sealed record GetDocumentContentResult(
    Stream ContentStream,
    string ContentType,
    string FileName);
