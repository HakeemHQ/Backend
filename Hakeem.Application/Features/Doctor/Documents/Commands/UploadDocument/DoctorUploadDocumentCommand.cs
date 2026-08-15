using MediatR;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Features.Doctor.Documents.Commands.UploadDocument;

public sealed class DoctorUploadDocumentCommand : IRequest<DoctorUploadDocumentResult>
{
    public Guid PatientProfileId { get; set; }
    public IFormFile? File { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DocumentDate { get; set; }
}

public sealed record DoctorUploadDocumentResult(
    Guid DocumentId,
    string DocumentType,
    string Title,
    DateOnly DocumentDate,
    string ExtractionStatus);
