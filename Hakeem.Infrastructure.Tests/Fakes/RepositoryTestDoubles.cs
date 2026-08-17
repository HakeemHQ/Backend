using Hakeem.Application.Common;
using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs;
using Hakeem.Application.Repositories.AuditLogs;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Services.Access;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;

namespace Hakeem.Infrastructure.Tests.Fakes;

internal sealed class FakeDoctorPatientAccessExpirationService
    : IDoctorPatientAccessExpirationService
{
    public Guid? DoctorProfileId { get; private set; }
    public Guid? PatientProfileId { get; private set; }
    public DateTime? UtcNow { get; private set; }
    public int CallCount { get; private set; }

    public Task ExpireForDoctorAsync(
        Guid doctorProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DoctorProfileId = doctorProfileId;
        UtcNow = utcNow;
        CallCount++;
        return Task.CompletedTask;
    }

    public Task ExpireForPatientAsync(
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PatientProfileId = patientProfileId;
        UtcNow = utcNow;
        CallCount++;
        return Task.CompletedTask;
    }

    public Task ExpireForPairAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DoctorProfileId = doctorProfileId;
        PatientProfileId = patientProfileId;
        UtcNow = utcNow;
        CallCount++;
        return Task.CompletedTask;
    }
}

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
    public Task<IReadOnlyList<DoctorProfile>> GetFilteredAsync(
        string? search,
        string? specialty,
        Hakeem.Domain.Enums.Identity.AccountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
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

    public Task<int> RevokeRedeemedRequestForAccessAsync(
        Guid accessId,
        Guid patientProfileId,
        DateTime revokedAt,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> ExpireActiveAccessesAsync(
        Guid? doctorProfileId,
        Guid? patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> ExpireRedeemedRequestsForExpiredAccessesAsync(
        Guid? doctorProfileId,
        Guid? patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> RevokeAllActiveForDoctorAsync(
        Guid doctorProfileId,
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
