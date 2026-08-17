using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Commands.RevokeDoctorPatientAccess;

public sealed class RevokeDoctorPatientAccessCommandHandler(
    IDoctorPatientAccessRepository accessRepository,
    IPatientProfileRepository patientProfileRepository,
    ICurrentUserContext currentUserContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeDoctorPatientAccessCommand>
{
    public async Task Handle(
        RevokeDoctorPatientAccessCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var revokedAt = DateTime.UtcNow;
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var affectedRows = await accessRepository.RevokeActiveForPatientAsync(
                request.AccessId,
                patient.Id,
                revokedAt,
                cancellationToken);

            if (affectedRows != 1)
            {
                throw new NotFoundException(
                    ErrorCodes.PatientAccessAccessNotFound);
            }

            var requestRows = await accessRepository
                .RevokeRedeemedRequestForAccessAsync(
                    request.AccessId,
                    patient.Id,
                    revokedAt,
                    cancellationToken);

            if (requestRows != 1)
            {
                throw new InvalidOperationException(
                    "The active access does not have one linked redeemed request.");
            }

            await unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await unitOfWork.RollBackTransactionAsync();
            throw;
        }
    }
}
