using Hakeem.Application.Common;
using Hakeem.Application.Features.Admin.Users.GetUsers.DTOs;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Users.GetUsers.Queries
{
    public record GetAdminUsersQuery(
     string? Search,
     AccountStatus? Status,
     int PageNumber = 1,
     int PageSize = 20
 ) : IRequest<PaginatedResult<AdminUserDto>>;
}
