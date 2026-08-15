using Hakeem.Application.Features.Admin.Users.GetUsers.DTOs;
using Hakeem.Application.Repositories.Users;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Users.GetUsers.Queries
{
    public sealed class GetAdminUsersQueryHandler(
    IUserRepository userRepository)
    : IRequestHandler<GetAdminUsersQuery, AdminUsersResponse>
    {
        public async Task<AdminUsersResponse> Handle(
            GetAdminUsersQuery request,
            CancellationToken cancellationToken)
        {
            var (items, totalCount) =
                await userRepository.GetUsersAsync(
                    request.Search,
                    request.UserType,
                    request.Status,
                    request.Page,
                    request.PageSize,
                    cancellationToken);

            return new AdminUsersResponse(items.ToArray());
        }
    }
}
