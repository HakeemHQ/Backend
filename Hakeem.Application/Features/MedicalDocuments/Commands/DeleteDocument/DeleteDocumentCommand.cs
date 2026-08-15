using MediatR;

namespace Hakeem.Application.Features.MedicalDocuments.Commands.DeleteDocument;

public sealed record DeleteDocumentCommand(Guid DocumentId) : IRequest;
