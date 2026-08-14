using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Repositories.AuditLogs
{
    public interface IAuditLogRepository : IScoped
    {
        Task<(IEnumerable<AdminAuditLogDto> Items, int TotalCount)> GetAuditLogsAsync(
            string? action,
            Guid? actorUserId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

        void Add(AuditLog auditLog);
    }

   
    }
