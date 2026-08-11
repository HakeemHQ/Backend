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
        var handler = new GetDoctorPatientAccessesQueryHandler(
            repository,
            new FakeDoctorProfileRepository(doctor),
            new FakeCurrentUserContext(doctor.UserId));

        var result = await handler.Handle(
            new GetDoctorPatientAccessesQuery(),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(access.Id, item.AccessId);
        Assert.Equal(patient.Id, item.PatientId);
        Assert.Equal(patient.PatientCode, item.PatientCode);
        Assert.Equal(patient.FullName, item.FullName);
        Assert.Equal(access.ExpiresAt, item.ExpiresAt);
        Assert.Equal(DoctorPatientAccessStatus.Active, repository.QueriedStatus);
        Assert.Equal(doctor.Id, repository.QueriedDoctorId);
    }

    [Fact]
    public async Task GetForPatient_ReturnsDoctorsWithCurrentAccess()
    {
        var doctor = CreateDoctor();
        var patient = CreatePatient();
        var access = CreateAccess(doctor.Id, patient);
        access.Doctor = doctor;
        var repository = new FakeAccessRepository([access]);
        var handler = new GetPatientDoctorAccessesQueryHandler(
            repository,
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId));

        var result = await handler.Handle(
            new GetPatientDoctorAccessesQuery(),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(access.Id, item.AccessId);
        Assert.Equal(doctor.Id, item.DoctorId);
        Assert.Equal("Dr. Ahmed Hassan", item.DoctorName);
        Assert.Equal("Cardiology", item.Specialty);
        Assert.Equal(patient.Id, repository.QueriedPatientId);
    }

    [Fact]
    public async Task Revoke_OwnActiveAccess_UsesAtomicConditionalUpdate()
    {
        var patient = CreatePatient();
        var repository = new FakeAccessRepository([]);
        var handler = new RevokeDoctorPatientAccessCommandHandler(
            repository,
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId));
        var accessId = Guid.NewGuid();

        await handler.Handle(
            new RevokeDoctorPatientAccessCommand(accessId),
            CancellationToken.None);

        Assert.Equal(accessId, repository.RevokedAccessId);
        Assert.Equal(patient.Id, repository.RevokingPatientId);
        Assert.NotNull(repository.RevokedAt);
        Assert.Equal(DateTimeKind.Utc, repository.RevokedAt.Value.Kind);
    }

    [Fact]
    public async Task Revoke_MissingExpiredOrForeignAccess_ReturnsNotFound()
    {
        var patient = CreatePatient();
        var repository = new FakeAccessRepository([])
        {
            RevokeAffectedRows = 0
        };
        var handler = new RevokeDoctorPatientAccessCommandHandler(
            repository,
            new FakePatientProfileRepository(patient),
            new FakeCurrentUserContext(patient.UserId));

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new RevokeDoctorPatientAccessCommand(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessAccessNotFound, exception.ErrorCode);
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
        public Guid? QueriedDoctorId { get; private set; }
        public Guid? QueriedPatientId { get; private set; }
        public DoctorPatientAccessStatus? QueriedStatus { get; private set; }
        public Guid? RevokedAccessId { get; private set; }
        public Guid? RevokingPatientId { get; private set; }
        public DateTime? RevokedAt { get; private set; }

        public Task<IReadOnlyList<DoctorPatientAccess>> GetForDoctorAsync(
            Guid doctorProfileId,
            DoctorPatientAccessStatus status,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            QueriedDoctorId = doctorProfileId;
            QueriedStatus = status;
            return Task.FromResult(accesses);
        }

        public Task<IReadOnlyList<DoctorPatientAccess>> GetActiveForPatientAsync(
            Guid patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            QueriedPatientId = patientProfileId;
            return Task.FromResult(accesses);
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
    }
}
