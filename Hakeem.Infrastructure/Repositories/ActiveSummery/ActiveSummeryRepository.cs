using Hakeem.Application.Features.Admin.ActiveSummery.DTOs;
using Hakeem.Application.Repositories.ActiveSummery;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Infrastructure.Repositories.ActiveSummery
{
    public sealed class ActivitySummaryRepository
      : IActivitySummaryRepository, IScoped
    {
        private readonly ApplicationDbContext _dbContext;

        public ActivitySummaryRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ActivitySummaryDto> GetActivitySummaryAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken)
        {
            var auditLogs = _dbContext.AuditLogs
                .AsNoTracking()
                .Where(x =>
                    x.OccurredAt >= fromDate &&
                    x.OccurredAt < toDate);

            var activePatients = await _dbContext.PatientProfiles
                .Where(x =>
                    x.User.Status == AccountStatus.Active &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDate.AddDays(1))
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            var activeDoctors = await _dbContext.DoctorProfiles
                .Where(x =>
                    x.User.Status == AccountStatus.Active &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDate.AddDays(1))
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            var documentsUploaded = await auditLogs
                .CountAsync(x => x.Action == "DocumentUploaded",
                    cancellationToken);

            var extractionsCompleted = await auditLogs
                .CountAsync(
                    x => x.Action == "DocumentExtractionCompleted",
                    cancellationToken);

            var medicalCvVersionsGenerated = await auditLogs
                .CountAsync(
                    x => x.Action == "MedicalCvVersionQueued",
                    cancellationToken);

            return new ActivitySummaryDto
            {
                FromDate = DateOnly.FromDateTime(fromDate),
                ToDate = DateOnly.FromDateTime(toDate.AddDays(-1)),
                ActivePatients = activePatients,
                ActiveDoctors = activeDoctors,
                DocumentsUploaded = documentsUploaded,
                ExtractionsCompleted = extractionsCompleted,
                MedicalCvVersionsGenerated = medicalCvVersionsGenerated
            };
        }
    }
}
