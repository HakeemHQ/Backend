using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.PatientAccessRequests.Queries.GetPatientAccessRequests;

public sealed class GetPatientAccessRequestsQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    IPatientAccessRequestRepository accessRequestRepository,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<GetPatientAccessRequestsQuery, GetPatientAccessRequestsResult>
{
    public async Task<GetPatientAccessRequestsResult> Handle(
        GetPatientAccessRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var requests = await accessRequestRepository.GetForPatientAsync(
            patient.Id,
            request.Status,
            cancellationToken);

        return new GetPatientAccessRequestsResult(
            requests.Select(accessRequest => new PatientAccessRequestItem(
                    accessRequest.Id,
                    new PatientAccessRequestDoctor(
                        accessRequest.Doctor.Id,
                        $"{accessRequest.Doctor.User.FirstName} {accessRequest.Doctor.User.LastName}".Trim(),
                        accessRequest.Doctor.Specialty),
                    accessRequest.Status.ToString(),
                    accessRequest.RequestedAt))
                .ToList());
    }
}
