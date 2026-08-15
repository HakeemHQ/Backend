using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs;
using Hakeem.Application.Repositories.AuditLogs;
using MediatR;


namespace Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.Queries
{
    public sealed class GetAdminAuditLogsQueryHandler(
     IAuditLogRepository auditLogRepository)
     : IRequestHandler<
         GetAdminAuditLogsQuery,
         AdminAuditLogsResponse>
    {
        public async Task<AdminAuditLogsResponse> Handle(
            GetAdminAuditLogsQuery request,
            CancellationToken cancellationToken)
        {
            var (items, totalCount) =
                await auditLogRepository.GetAuditLogsAsync(
                    request.Action,
                    request.ActorUserId,
                    request.FromDate?.ToDateTime(TimeOnly.MinValue),
                    request.ToDate?.AddDays(1).ToDateTime(TimeOnly.MinValue),
                    request.Page,
                    request.PageSize,
                    cancellationToken);

            return new AdminAuditLogsResponse(items.ToArray());
        }
    }
}
