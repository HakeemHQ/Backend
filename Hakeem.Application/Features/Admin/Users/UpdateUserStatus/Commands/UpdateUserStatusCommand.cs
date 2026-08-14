using Hakeem.Domain.Enums.Identity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Users.UpdateUserStatus.Commands
{
    public sealed record UpdateUserStatusCommand(Guid UserId,string Status): IRequest<UpdateUserStatusResult>;

    public sealed record UpdateUserStatusResult(Guid UserId,string Email,ApplicationRole UserType,AccountStatus Status);


}
