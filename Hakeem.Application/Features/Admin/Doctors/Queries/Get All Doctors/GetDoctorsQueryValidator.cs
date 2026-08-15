using FluentValidation;
using Hakeem.Application.Validators.Extensions;

namespace Hakeem.Application.Features.Admin.Doctors.Queries;

public sealed class GetDoctorsQueryValidator : AbstractValidator<GetDoctorsQuery>
{
    public GetDoctorsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}

