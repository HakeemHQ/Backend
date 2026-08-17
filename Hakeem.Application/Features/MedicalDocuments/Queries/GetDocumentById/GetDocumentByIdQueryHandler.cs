using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Documents;
using MediatR;

namespace Hakeem.Application.Features.MedicalDocuments.Queries.GetDocumentById;

public sealed class GetDocumentByIdQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalDocumentRepository medicalDocumentRepository,
    IFileUrlResolver fileUrlResolver)
    : IRequestHandler<GetDocumentByIdQuery, GetDocumentByIdResult>
{
    public async Task<GetDocumentByIdResult> Handle(
        GetDocumentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var patientProfile = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patientProfile is null)
        {
            throw new UnAuthorizedException(ErrorCodes.DocumentPatientProfileNotFound);
        }

        var document = await medicalDocumentRepository.GetByIdForPatientAsync(
            request.DocumentId,
            patientProfile.Id,
            cancellationToken);

        if (document is null)
        {
            throw new NotFoundException(ErrorCodes.DocumentNotFound);
        }

        if (document.ExtractionStatus == ExtractionStatus.Rejected)
        {
            throw new UnprocessableEntityException(
                ErrorCodes.DocumentNotMedical);
        }

        return new GetDocumentByIdResult(
            document.Id,
            document.DocumentType,
            document.Title,
            DateOnly.FromDateTime(document.DocumentDate),
            document.ExtractionStatus.ToString(),
            document.ReviewStatus.ToString(),
            document.FailureCode,
            fileUrlResolver.ResolveFileUrl(document.FilePath));
    }
}
