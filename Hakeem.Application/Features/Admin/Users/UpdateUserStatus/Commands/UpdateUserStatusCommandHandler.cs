using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Auth;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.Users;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Users.UpdateUserStatus.Commands
{
    public sealed class UpdateUserStatusCommandHandler(
     IUserRepository userRepository,
     IDoctorProfileRepository doctorProfileRepository,
     IDoctorPatientAccessRepository accessRepository,
     IRefreshTokenRepository refreshTokenRepository,
     IUnitOfWork unitOfWork)
     : IRequestHandler<UpdateUserStatusCommand, UpdateUserStatusResult>
    {
        public async Task<UpdateUserStatusResult> Handle(
            UpdateUserStatusCommand request,
            CancellationToken cancellationToken)
        {
            if (!Enum.GetNames<AccountStatus>().Contains(
                    request.Status,
                    StringComparer.OrdinalIgnoreCase) ||
                !Enum.TryParse<AccountStatus>(
                    request.Status,
                    true,
                    out var status) ||
                !Enum.IsDefined(status))
            {
                throw new UnprocessableEntityException(
                    ErrorCodes.AccountInvalidStatusTransition);
            }

            var user = await userRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                throw new NotFoundException(ErrorCodes.UserNotFound);
            }

            if (user.Status == status)
            {
                throw new UnprocessableEntityException(
                    ErrorCodes.AccountInvalidStatusTransition);
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                user.Status = status;

                if (status == AccountStatus.Suspended)
                {
                    var now = DateTime.UtcNow;
                    await refreshTokenRepository.RevokeAllActiveForUserAsync(
                        user.Id,
                        now,
                        cancellationToken);

                    if (user.Role == ApplicationRole.Doctor)
                    {
                        var doctor = await doctorProfileRepository.GetByUserIdAsync(
                            user.Id,
                            cancellationToken);

                        if (doctor is not null)
                        {
                            await accessRepository.RevokeAllActiveForDoctorAsync(
                                doctor.Id,
                                now,
                                cancellationToken);
                        }
                    }
                }

                await unitOfWork.SaveChanges(cancellationToken);
                await unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await unitOfWork.RollBackTransactionAsync();
                throw;
            }

            return new UpdateUserStatusResult(
                user.Id,
                user.Status.ToString());
        }
    }
}
