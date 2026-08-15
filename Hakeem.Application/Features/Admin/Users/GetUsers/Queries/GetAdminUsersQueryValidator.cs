using FluentValidation;

namespace Hakeem.Application.Features.Admin.Users.GetUsers.Queries;

public sealed class GetAdminUsersQueryValidator
    : AbstractValidator<GetAdminUsersQuery>
{
    public GetAdminUsersQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}

