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
     IUnitOfWork unitOfWork)
     : IRequestHandler<UpdateUserStatusCommand, UpdateUserStatusResult>
    {
        public async Task<UpdateUserStatusResult> Handle(
            UpdateUserStatusCommand request,
            CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<AccountStatus>(
                    request.Status,
                    true,
                    out var status))
            {
                throw new ArgumentException("Unsupported user status.");
            }

            var user = await userRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException("User does not exist.");
            }

            user.Status = status;

            await unitOfWork.SaveChanges(cancellationToken);

            return new UpdateUserStatusResult(
                user.Id,
                user.Email,
                user.Role,
                user.Status);
        }
    }
}
