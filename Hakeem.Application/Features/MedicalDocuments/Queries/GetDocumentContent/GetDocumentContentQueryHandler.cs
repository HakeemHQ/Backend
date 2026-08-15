using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;
using Microsoft.AspNetCore.StaticFiles;

namespace Hakeem.Application.Features.MedicalDocuments.Queries.GetDocumentContent;

public sealed class GetDocumentContentQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IDoctorProfileRepository doctorProfileRepository,
    IDoctorPatientAccessRepository doctorPatientAccessRepository,
    IMedicalDocumentRepository medicalDocumentRepository,
    IDocumentContentProvider documentContentProvider)
    : IRequestHandler<GetDocumentContentQuery, GetDocumentContentResult>
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public async Task<GetDocumentContentResult> Handle(
        GetDocumentContentQuery request,
        CancellationToken cancellationToken)
    {
        var document = await medicalDocumentRepository.GetByIdAsync(
            request.DocumentId,
            cancellationToken);

        if (document is null)
        {
            throw new NotFoundException(ErrorCodes.DocumentNotFound);
        }

        if (!await CanAccessDocumentAsync(document.PatientProfileId, cancellationToken))
        {
            throw new NotFoundException(ErrorCodes.DocumentNotFound);
        }

        var stream = await documentContentProvider.OpenReadAsync(
            document.FilePath,
            cancellationToken);

        var contentType = ResolveContentType(document.FilePath);
        var fileName = Path.GetFileName(document.FilePath);

        return new GetDocumentContentResult(stream, contentType, fileName);
    }

    private async Task<bool> CanAccessDocumentAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is not null && patient.Id == patientProfileId)
        {
            return true;
        }

        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            return false;
        }

        return await doctorPatientAccessRepository.HasActiveAccessAsync(
            doctor.Id,
            patientProfileId,
            DateTime.UtcNow,
            cancellationToken);
    }

    private static string ResolveContentType(string filePath)
    {
        if (ContentTypeProvider.TryGetContentType(filePath, out var contentType))
        {
            return contentType;
        }

        return "application/octet-stream";
    }
}
