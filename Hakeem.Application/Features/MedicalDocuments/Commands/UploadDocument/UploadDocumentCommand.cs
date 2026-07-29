using MediatR;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Features.MedicalDocuments.Commands.UploadDocument;

public sealed class UploadDocumentCommand : IRequest<UploadDocumentResult>
{
    public IFormFile? File { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateOnly DocumentDate { get; set; }
}

public sealed record UploadDocumentResult(
    Guid DocumentId,
    string DocumentType,
    string Title,
    DateOnly DocumentDate,
    string ExtractionStatus);
