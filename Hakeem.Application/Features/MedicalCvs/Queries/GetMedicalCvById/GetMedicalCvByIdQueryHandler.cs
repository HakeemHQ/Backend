using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvById;

public sealed class GetMedicalCvByIdQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalCvReadRepository medicalCvReadRepository)
    : IRequestHandler<GetMedicalCvByIdQuery, GetMedicalCvByIdResponse>
{
    public async Task<GetMedicalCvByIdResponse> Handle(
        GetMedicalCvByIdQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var medicalCv = await medicalCvReadRepository.GetDetailForPatientAsync(
            request.MedicalCvId,
            patient.Id,
            cancellationToken);

        if (medicalCv is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvNotFound);
        }

        var versions = medicalCv.Versions
            .Select(version => new MedicalCvVersionDetailResponse(
                version.MedicalCvVersionId,
                version.VersionNumber,
                version.Status,
                version.CreatedAt,
                version.ApprovedAt,
                version.PdfAvailable))
            .ToList();

        return new GetMedicalCvByIdResponse(
            medicalCv.MedicalCvId,
            medicalCv.Title,
            medicalCv.ScopeType,
            medicalCv.Focus,
            medicalCv.CreatedAt,
            medicalCv.UpdatedAt,
            versions);
    }
}
