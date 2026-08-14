using Hakeem.Application.Common;
using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs;
using Hakeem.Application.Repositories.AuditLogs;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;

namespace Hakeem.Infrastructure.Tests.Fakes;

internal sealed class FakeAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> AddedAuditLogs { get; } = [];

    public void Add(AuditLog auditLog) => AddedAuditLogs.Add(auditLog);

    public Task<(IEnumerable<AdminAuditLogDto> Items, int TotalCount)> GetAuditLogsAsync(
        string? action,
        Guid? actorUserId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class NullDoctorProfileRepository : IDoctorProfileRepository
{
    public Task<DoctorProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<DoctorProfile?>(null);
    public void Add(DoctorProfile doctorProfile) => throw new NotSupportedException();
    public Task<bool> LicenseNumberExistsAsync(string licenseNumber, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    public Task<IReadOnlyList<DoctorProfile>> GetAllAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    public Task<DoctorProfile?> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    public Task<DoctorProfile?> GetByIdForUpdateAsync(Guid doctorId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class NullDoctorPatientAccessRepository : IDoctorPatientAccessRepository
{
    public Task<PaginatedResult<DoctorPatientAccess>> GetForDoctorAsync(
        Guid doctorProfileId,
        DoctorPatientAccessStatus status,
        DateTime utcNow,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<PaginatedResult<DoctorPatientAccess>> GetActiveForPatientAsync(
        Guid patientProfileId,
        DateTime utcNow,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> RevokeActiveForPatientAsync(
        Guid accessId,
        Guid patientProfileId,
        DateTime revokedAt,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> HasActiveAccessAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
