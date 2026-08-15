using FluentValidation;

namespace Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.Queries;

public sealed class GetAdminAuditLogsQueryValidator
    : AbstractValidator<GetAdminAuditLogsQuery>
{
    public GetAdminAuditLogsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query =>
                !query.FromDate.HasValue ||
                !query.ToDate.HasValue ||
                query.FromDate <= query.ToDate);
    }
}

