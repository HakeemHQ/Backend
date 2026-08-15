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
     string? Action = null,
     Guid? ActorUserId = null,
     DateOnly? FromDate = null,
     DateOnly? ToDate = null,
     int Page = 1,
     int PageSize = 20)
     : IRequest<AdminAuditLogsResponse>;
}
