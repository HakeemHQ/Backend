using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs
{
    public sealed class AdminAuditLogDto
    {
        public Guid AuditLogId { get; init; }
        public Guid? ActorUserId { get; init; }
        public string Action { get; init; } = string.Empty;
        public string Target { get; init; } = string.Empty;
        public DateTime OccurredAt { get; init; }
    }

    public sealed record AdminAuditLogsResponse(
        IReadOnlyList<AdminAuditLogDto> Items);
}
