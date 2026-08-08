using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace Hakeem.Application.Features.MedicalRecords.Queries.SearchMedicalRecords;

public sealed class SearchMedicalRecordsQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalRecordSearchService medicalRecordSearchService)
    : IRequestHandler<SearchMedicalRecordsQuery, IReadOnlyList<MedicalRecordSearchResult>>
{
    public async Task<IReadOnlyList<MedicalRecordSearchResult>> Handle(
        SearchMedicalRecordsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ValidationException("Query is required.");
        }

        if (request.PatientProfileId == Guid.Empty)
        {
            throw new ValidationException("PatientProfileId is required.");
        }

        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        if (patient.Id != request.PatientProfileId)
        {
            throw new NotFoundException(ErrorCodes.DocumentPatientProfileNotFound);
        }

        return await medicalRecordSearchService.SearchAsync(
            request.Query,
            request.PatientProfileId,
            request.Limit,
            cancellationToken);
    }
}
