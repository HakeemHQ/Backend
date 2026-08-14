using Hakeem.Application.Common;
using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs;
using Hakeem.Application.Repositories.AuditLogs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.Queries
{
    public sealed class GetAdminAuditLogsQueryHandler(
     IAuditLogRepository auditLogRepository)
     : IRequestHandler<
         GetAdminAuditLogsQuery,
         PaginatedResult<AdminAuditLogDto>>
    {
        public async Task<PaginatedResult<AdminAuditLogDto>> Handle(
            GetAdminAuditLogsQuery request,
            CancellationToken cancellationToken)
        {
            var (items, totalCount) =
                await auditLogRepository.GetAuditLogsAsync(
                    request.Action,
                    request.ActorUserId,
                    request.FromDate,
                    request.ToDate,
                    request.Page,
                    request.PageSize,
                    cancellationToken);

            return new PaginatedResult<AdminAuditLogDto>(
                items,
                totalCount,
                request.Page,
                request.PageSize);
        }
    }
}
