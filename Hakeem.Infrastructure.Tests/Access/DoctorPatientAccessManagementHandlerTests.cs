using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.DoctorPatientAccesses.Commands.RevokeDoctorPatientAccess;
using Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetDoctorPatientAccesses;
using Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetPatientDoctorAccesses;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;
using Hakeem.Infrastructure.Tests.Fakes;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class DoctorPatientAccessManagementHandlerTests
{
    [Fact]
    public async Task GetForDoctor_ReturnsAccessiblePatientDetails()
    {
        var doctor = CreateDoctor();
        var patient = CreatePatient();
        var access = CreateAccess(doctor.Id, patient);
        var repository = new FakeAccessRepository([access]);
        var expiration = new FakeDoctorPatientAccessExpirationService();
        var handler = new GetDoctorPatientAccessesQueryHandler(
            repository,
            new FakeDoctorProfileRepository(doctor),
            new FakeCurrentUserContext(doctor.UserId),
            expiration);

        var result = await handler.Handle(
            new GetDoctorPatientAccessesQuery(
                DoctorPatientAccessStatus.Active,
                PageNumber: 2,
                PageSize: 5),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(access.Id, item.AccessId);
        Assert.Equal(patient.Id, item.PatientId);
        Assert.Equal(patient.PatientCode, item.PatientCode);
        Assert.Equal(patient.FullName, item.FullName);
        Assert.Equal(access.ExpiresAt, item.ExpiresAt);
        Assert.Equal(DoctorPatientAccessStatus.Active, repository.QueriedStatus);
        Assert.Equal(doctor.Id, repository.QueriedDoctorId);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(doctor.Id, expiration.DoctorProfileId);
    }

    [Fact]
    public async Task GetForPatient_ReturnsDoctorsWithCurrentAccess()
    {
        var doctor = CreateDoctor();
        var patient = CreatePatient();
        var access = CreateAccess(doctor.Id, patient);
        access.Doctor = doctor;
        var repository = new FakeAccessRepository([access]);
        var expiration = new FakeDoctorPatientAccessExpirationService();
        var handler = new GetPatientDoctorAccessesQueryHandler(
            repository,
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId),
            expiration);

        var result = await handler.Handle(
            new GetPatientDoctorAccessesQuery(PageNumber: 3, PageSize: 4),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(access.Id, item.AccessId);
        Assert.Equal(doctor.Id, item.DoctorId);
        Assert.Equal("Dr. Ahmed Hassan", item.DoctorName);
        Assert.Equal("Cardiology", item.Specialty);
        Assert.Equal(patient.Id, repository.QueriedPatientId);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(4, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(patient.Id, expiration.PatientProfileId);
    }

    [Fact]
    public async Task Revoke_OwnActiveAccess_UsesAtomicConditionalUpdate()
    {
        var patient = CreatePatient();
        var repository = new FakeAccessRepository([]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RevokeDoctorPatientAccessCommandHandler(
            repository,
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId),
            unitOfWork);
        var accessId = Guid.NewGuid();

        await handler.Handle(
            new RevokeDoctorPatientAccessCommand(accessId),
            CancellationToken.None);

        Assert.Equal(accessId, repository.RevokedAccessId);
        Assert.Equal(patient.Id, repository.RevokingPatientId);
        Assert.NotNull(repository.RevokedAt);
        Assert.Equal(DateTimeKind.Utc, repository.RevokedAt.Value.Kind);
        Assert.Equal(accessId, repository.RevokedRequestAccessId);
        Assert.Equal(repository.RevokedAt, repository.RequestRevokedAt);
        Assert.Equal(1, unitOfWork.BeginTransactionCallCount);
        Assert.Equal(1, unitOfWork.CommitTransactionCallCount);
        Assert.Equal(0, unitOfWork.RollbackTransactionCallCount);
    }

    [Fact]
    public async Task Revoke_MissingExpiredOrForeignAccess_ReturnsNotFound()
    {
        var patient = CreatePatient();
        var repository = new FakeAccessRepository([])
        {
            RevokeAffectedRows = 0
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RevokeDoctorPatientAccessCommandHandler(
            repository,
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new RevokeDoctorPatientAccessCommand(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessAccessNotFound, exception.ErrorCode);
        Assert.Null(repository.RevokedRequestAccessId);
        Assert.Equal(1, unitOfWork.RollbackTransactionCallCount);
    }

    [Fact]
    public async Task Revoke_LinkedRequestUpdateFails_RollsBackAccessRevocation()
    {
        var patient = CreatePatient();
        var repository = new FakeAccessRepository([])
        {
            RevokeRequestAffectedRows = 0
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RevokeDoctorPatientAccessCommandHandler(
            repository,
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId),
            unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new RevokeDoctorPatientAccessCommand(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(0, unitOfWork.CommitTransactionCallCount);
        Assert.Equal(1, unitOfWork.RollbackTransactionCallCount);
    }

    private static DoctorProfile CreateDoctor() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Specialty = "Cardiology",
        User = new User
        {
            Status = AccountStatus.Active,
            FirstName = "Dr. Ahmed",
            LastName = "Hassan"
        }
    };

    private static PatientProfile CreatePatient() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        PatientCode = "H89K-27P",
        FullName = "Mazen Mohamed",
        User = new User { Status = AccountStatus.Active }
    };

    private static DoctorPatientAccess CreateAccess(
        Guid doctorId,
        PatientProfile patient) => new()
    {
        Id = Guid.NewGuid(),
        DoctorProfileId = doctorId,
        PatientProfileId = patient.Id,
        Patient = patient,
        Status = DoctorPatientAccessStatus.Active,
        GrantedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddHours(2)
    };

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakeDoctorProfileRepository(DoctorProfile doctor)
        : IDoctorProfileRepository
    {
        public Task<DoctorProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(doctor.UserId == userId ? doctor : null);

        public void Add(DoctorProfile doctorProfile) => throw new NotSupportedException();
        public Task<bool> LicenseNumberExistsAsync(string licenseNumber, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<DoctorProfile>> GetAllAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<DoctorProfile?> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(doctor.Id == doctorId ? doctor : null);
        public Task<DoctorProfile?> GetByIdForUpdateAsync(Guid doctorId, CancellationToken cancellationToken) =>
            Task.FromResult<DoctorProfile?>(doctor.Id == doctorId ? doctor : null);
        public Task<IReadOnlyList<DoctorProfile>> GetFilteredAsync(
            string? search,
            string? specialty,
            Hakeem.Domain.Enums.Identity.AccountStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePatientProfileRepository(PatientProfile patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(patient.Id == patientProfileId ? patient : null);

        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(patient.UserId == userId ? patient : null);

        public Task<PatientProfile?> UpdateByUserIdAsync(
            Guid userId,
            string? fullName,
            DateTime? birthDate,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            string? gender,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAccessRepository(
        IReadOnlyList<DoctorPatientAccess> accesses)
        : IDoctorPatientAccessRepository
    {
        public int RevokeAffectedRows { get; init; } = 1;
        public int RevokeRequestAffectedRows { get; init; } = 1;
        public Guid? QueriedDoctorId { get; private set; }
        public Guid? QueriedPatientId { get; private set; }
        public DoctorPatientAccessStatus? QueriedStatus { get; private set; }
        public Guid? RevokedAccessId { get; private set; }
        public Guid? RevokingPatientId { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public Guid? RevokedRequestAccessId { get; private set; }
        public DateTime? RequestRevokedAt { get; private set; }

        public Task<Hakeem.Application.Common.PaginatedResult<DoctorPatientAccess>> GetForDoctorAsync(
            Guid doctorProfileId,
            DoctorPatientAccessStatus status,
            DateTime utcNow,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            QueriedDoctorId = doctorProfileId;
            QueriedStatus = status;
            return Task.FromResult(
                new Hakeem.Application.Common.PaginatedResult<DoctorPatientAccess>(
                    accesses,
                    accesses.Count,
                    pageNumber,
                    pageSize));
        }

        public Task<Hakeem.Application.Common.PaginatedResult<DoctorPatientAccess>> GetActiveForPatientAsync(
            Guid patientProfileId,
            DateTime utcNow,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            QueriedPatientId = patientProfileId;
            return Task.FromResult(
                new Hakeem.Application.Common.PaginatedResult<DoctorPatientAccess>(
                    accesses,
                    accesses.Count,
                    pageNumber,
                    pageSize));
        }

        public Task<int> RevokeActiveForPatientAsync(
            Guid accessId,
            Guid patientProfileId,
            DateTime revokedAt,
            CancellationToken cancellationToken)
        {
            RevokedAccessId = accessId;
            RevokingPatientId = patientProfileId;
            RevokedAt = revokedAt;
            return Task.FromResult(RevokeAffectedRows);
        }

        public Task<int> RevokeRedeemedRequestForAccessAsync(
            Guid accessId,
            Guid patientProfileId,
            DateTime revokedAt,
            CancellationToken cancellationToken)
        {
            RevokedRequestAccessId = accessId;
            RequestRevokedAt = revokedAt;
            return Task.FromResult(RevokeRequestAffectedRows);
        }

        public Task<int> ExpireActiveAccessesAsync(
            Guid? doctorProfileId,
            Guid? patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<int> ExpireRedeemedRequestsForExpiredAccessesAsync(
            Guid? doctorProfileId,
            Guid? patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<int> RevokeAllActiveForDoctorAsync(
            Guid doctorProfileId,
            DateTime revokedAt,
            CancellationToken cancellationToken) =>
            Task.FromResult(RevokeAffectedRows);

        public Task<bool> HasActiveAccessAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            Task.FromResult(accesses.Any(access =>
                access.DoctorProfileId == doctorProfileId &&
                access.PatientProfileId == patientProfileId &&
                access.Status == DoctorPatientAccessStatus.Active &&
                access.ExpiresAt > utcNow));
    }
}
