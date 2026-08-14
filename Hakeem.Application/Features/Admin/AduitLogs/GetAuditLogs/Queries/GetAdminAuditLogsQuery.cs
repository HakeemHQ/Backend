using Hakeem.Application.Common;
using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.Queries
{
    public sealed record GetAdminAuditLogsQuery(
     string? Action,
     Guid? ActorUserId,
     DateTime? FromDate,
     DateTime? ToDate,
     int Page = 1,
     int PageSize = 20)
     : IRequest<PaginatedResult<AdminAuditLogDto>>;
}
