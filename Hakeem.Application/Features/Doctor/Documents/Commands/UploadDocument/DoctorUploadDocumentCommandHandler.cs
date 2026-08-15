using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Services.Access;
using Hakeem.Application.Services.Files;
using MediatR;

namespace Hakeem.Application.Features.Doctor.Documents.Commands.UploadDocument;

public sealed class DoctorUploadDocumentCommandHandler(
    ICurrentUserContext currentUserContext,
    IDoctorPatientAccessGuard doctorPatientAccessGuard,
    IDocumentUploadOrchestrator documentUploadOrchestrator)
    : IRequestHandler<DoctorUploadDocumentCommand, DoctorUploadDocumentResult>
{
    public async Task<DoctorUploadDocumentResult> Handle(
        DoctorUploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        await doctorPatientAccessGuard.RequireDoctorWithActiveAccessAsync(
            request.PatientProfileId,
            cancellationToken);

        var result = await documentUploadOrchestrator.UploadAsync(
            request.PatientProfileId,
            currentUserContext.UserId,
            request.File!,
            request.Title,
            request.DocumentDate,
            cancellationToken);

        return new DoctorUploadDocumentResult(
            result.DocumentId,
            result.DocumentType,
            result.Title,
            result.DocumentDate,
            result.ExtractionStatus);
    }
}
