using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvs;

public sealed class GetMedicalCvsQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalCvReadRepository medicalCvReadRepository)
    : IRequestHandler<GetMedicalCvsQuery, GetMedicalCvsResponse>
{
    public async Task<GetMedicalCvsResponse> Handle(
        GetMedicalCvsQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var result = await medicalCvReadRepository.GetForPatientAsync(
            patient.Id,
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);

        var items = result.Items
            .Select(medicalCv => new MedicalCvListItemResponse(
                medicalCv.MedicalCvId,
                medicalCv.Title,
                medicalCv.ScopeType,
                medicalCv.Focus,
                medicalCv.LatestVersion is null
                    ? null
                    : new LatestMedicalCvVersionResponse(
                        medicalCv.LatestVersion.MedicalCvVersionId,
                        medicalCv.LatestVersion.VersionNumber,
                        medicalCv.LatestVersion.Status)))
            .ToList();

        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);

        return new GetMedicalCvsResponse(
            items,
            new PaginationResponse(
                result.PageNumber,
                result.PageSize,
                result.TotalCount,
                totalPages));
    }
}
