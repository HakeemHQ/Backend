using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Auth;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Features.Admin.Doctors.Commands.UpdateDoctorStatus
{
    public sealed class UpdateDoctorStatusCommandHandler(
     IDoctorProfileRepository doctorProfileRepository,
     IDoctorPatientAccessRepository accessRepository,
     IRefreshTokenRepository refreshTokenRepository,
     IUnitOfWork unitOfWork)
     : IRequestHandler<UpdateDoctorStatusCommand, UpdateDoctorStatusResult>
    {
        public async Task<UpdateDoctorStatusResult> Handle(
            UpdateDoctorStatusCommand request,
            CancellationToken cancellationToken)
        {
            var doctor = await doctorProfileRepository.GetByIdForUpdateAsync(
                request.DoctorId,
                cancellationToken);

            if (doctor is null)
            {
                throw new NotFoundException(ErrorCodes.DoctorNotFound);
            }

            if (!Enum.GetNames<AccountStatus>().Contains(
                    request.Status,
                    StringComparer.OrdinalIgnoreCase) ||
                !Enum.TryParse<AccountStatus>(
                    request.Status,
                    ignoreCase: true,
                    out var requestedStatus) ||
                !Enum.IsDefined(requestedStatus) ||
                doctor.User.Status == requestedStatus)
            {
                throw new UnprocessableEntityException(
                    ErrorCodes.AccountInvalidStatusTransition);
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                doctor.User.Status = requestedStatus;

                if (requestedStatus == AccountStatus.Suspended)
                {
                    var now = DateTime.UtcNow;
                    await accessRepository.RevokeAllActiveForDoctorAsync(
                        doctor.Id,
                        now,
                        cancellationToken);
                    await refreshTokenRepository.RevokeAllActiveForUserAsync(
                        doctor.UserId,
                        now,
                        cancellationToken);
                }

                await unitOfWork.SaveChanges(cancellationToken);
                await unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await unitOfWork.RollBackTransactionAsync();
                throw;
            }

            return new UpdateDoctorStatusResult(
                doctor.Id,
                doctor.User.Status.ToString());
        }
    }
}
