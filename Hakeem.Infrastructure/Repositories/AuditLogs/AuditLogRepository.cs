using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs;
using Hakeem.Application.Repositories.AuditLogs;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Infrastructure.Repositories.AuditLogs
{
    public sealed class AuditLogRepository : IAuditLogRepository, IScoped
    {
        private readonly ApplicationDbContext _dbContext;

        public void Add(AuditLog auditLog)
        {
            _dbContext.AuditLogs.Add(auditLog);
        }

        public AuditLogRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(IEnumerable<AdminAuditLogDto> Items, int TotalCount)> GetAuditLogsAsync(
            string? action,
            Guid? actorUserId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = _dbContext.AuditLogs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(x => x.Action == action);
            }

            if (actorUserId.HasValue)
            {
                query = query.Where(x =>
                    x.ActorUserId == actorUserId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x =>
                    x.OccurredAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x =>
                    x.OccurredAt <= toDate.Value);
            }

            var totalCount =
                await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminAuditLogDto
                {
                    AuditLogId = x.Id,
                    ActorUserId = x.ActorUserId,
                    PatientProfileId = x.PatientProfileId,
                    Action = x.Action,
                    Target = x.Target,
                    OccurredAt = x.OccurredAt
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
