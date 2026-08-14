using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.PatientIdentities.Commands.VerifyPatientIdentity;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.PatientIdentities;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Tests.Identity;

public sealed class VerifyPatientIdentityCommandHandlerTests
{
    private const string NationalId = "30005120112345";

    [Fact]
    public async Task Handle_VerifiesIdentityTransactionallyAndCorrectsClaimWithoutGrantingAccess()
    {
        var doctor = CreateDoctor();
        var patient = CreatePatient(nationalIdClaim: "30005120199995");
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(patient, doctor, unitOfWork);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(unitOfWork.TransactionStarted);
        Assert.True(unitOfWork.SaveCalled);
        Assert.True(unitOfWork.Committed);
        Assert.False(unitOfWork.RolledBack);
        Assert.Equal(NationalId, patient.NationalId);
        Assert.Equal(NationalId, patient.VerifiedNationalId);
        Assert.Equal(doctor.Id, patient.VerifiedByDoctorId);
        Assert.Equal(IdentityVerificationStatus.Verified, patient.IdentityVerificationStatus);
        Assert.NotNull(patient.VerifiedAt);
        Assert.Equal(DateTimeKind.Utc, patient.VerifiedAt.Value.Kind);
        Assert.True(result.ClaimCorrected);
        Assert.Equal("Verified", result.IdentityVerificationStatus);
        Assert.Empty(patient.DoctorAccesses);
        Assert.Null(typeof(VerifyPatientIdentityResult).GetProperty("NationalId"));
        Assert.Null(typeof(VerifyPatientIdentityResult).GetProperty("VerifiedNationalId"));
    }

    [Fact]
    public async Task Handle_WhenClaimMatches_ReturnsClaimCorrectedFalse()
    {
        var patient = CreatePatient(NationalId);
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(patient, CreateDoctor(), unitOfWork);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.ClaimCorrected);
        Assert.True(unitOfWork.Committed);
    }

    [Fact]
    public async Task Handle_WhenAlreadyVerifiedForSamePatient_ReturnsExistingVerification()
    {
        var verifiedAt = DateTime.UtcNow.AddDays(-1);
        var patient = CreatePatient(NationalId);
        patient.IdentityVerificationStatus = IdentityVerificationStatus.Verified;
        patient.VerifiedNationalId = NationalId;
        patient.VerifiedAt = verifiedAt;
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(patient, CreateDoctor(), unitOfWork);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(verifiedAt, result.VerifiedAt);
        Assert.False(result.ClaimCorrected);
        Assert.True(unitOfWork.Committed);
        Assert.False(unitOfWork.SaveCalled);
    }

    [Fact]
    public async Task Handle_WhenPatientCodeIsUnknown_ReturnsNotFoundAndRollsBack()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(patient: null, CreateDoctor(), unitOfWork);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientIdentityNotFound, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
    }

    [Fact]
    public async Task Handle_WhenNationalIdDoesNotMatchBirthDate_ReturnsUnprocessableAndRollsBack()
    {
        var patient = CreatePatient(NationalId);
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(patient, CreateDoctor(), unitOfWork);
        var command = CreateCommand();
        command.NationalId = "30005130112345";

        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientIdentityNationalIdMismatch, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
        Assert.False(unitOfWork.SaveCalled);
    }

    [Fact]
    public async Task Handle_WhenNationalIdBelongsToAnotherPatient_ReturnsConflictAndRollsBack()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            CreatePatient(NationalId),
            CreateDoctor(),
            unitOfWork,
            nationalIdBelongsToAnotherPatient: true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientIdentityNationalIdAlreadyVerified, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
        Assert.False(unitOfWork.SaveCalled);
    }

    [Fact]
    public async Task Handle_WhenPatientIsVerifiedWithDifferentNationalId_ReturnsConflict()
    {
        var patient = CreatePatient(NationalId);
        patient.IdentityVerificationStatus = IdentityVerificationStatus.Verified;
        patient.VerifiedNationalId = "30005120199995";
        patient.VerifiedAt = DateTime.UtcNow;
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(patient, CreateDoctor(), unitOfWork);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(
            ErrorCodes.PatientIdentityAlreadyVerifiedWithDifferentNationalId,
            exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
    }

    [Fact]
    public async Task Handle_WhenDatabaseUniqueIndexWinsRace_ReturnsConflictAndRollsBack()
    {
        var unitOfWork = new FakeUnitOfWork
        {
            SaveException = new DbUpdateException(
                "Violation of unique index IX_PatientProfiles_VerifiedNationalId")
        };
        var handler = CreateHandler(
            CreatePatient(NationalId),
            CreateDoctor(),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientIdentityNationalIdAlreadyVerified, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
        Assert.False(unitOfWork.Committed);
    }

    private static VerifyPatientIdentityCommandHandler CreateHandler(
        PatientProfile? patient,
        DoctorProfile doctor,
        FakeUnitOfWork unitOfWork,
        bool nationalIdBelongsToAnotherPatient = false)
    {
        return new VerifyPatientIdentityCommandHandler(
            new FakePatientIdentityRepository(
                patient,
                nationalIdBelongsToAnotherPatient,
                unitOfWork),
            new FakeDoctorProfileRepository(doctor),
            new FakeCurrentUserContext(doctor.UserId),
            unitOfWork);
    }

    private static VerifyPatientIdentityCommand CreateCommand() => new()
    {
        PatientCode = "h89k-27p",
        NationalId = NationalId
    };

    private static DoctorProfile CreateDoctor() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid()
    };

    private static PatientProfile CreatePatient(string nationalIdClaim) => new()
    {
        Id = Guid.NewGuid(),
        PatientCode = "H89K-27P",
        FullName = "Mazen Mohamed",
        BirthDate = new DateTime(2000, 5, 12),
        NationalId = nationalIdClaim,
        IdentityVerificationStatus = IdentityVerificationStatus.Pending
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
    }

    private sealed class FakePatientIdentityRepository(
        PatientProfile? patient,
        bool belongsToAnotherPatient,
        FakeUnitOfWork unitOfWork)
        : IPatientIdentityRepository
    {
        public Task<PatientProfile?> GetByPatientCodeForUpdateAsync(
            string patientCode,
            CancellationToken cancellationToken)
        {
            Assert.True(unitOfWork.TransactionStarted);
            Assert.False(unitOfWork.Committed);
            return Task.FromResult(
                patient?.PatientCode == patientCode ? patient : null);
        }

        public Task<bool> VerifiedNationalIdBelongsToAnotherPatientAsync(
            string verifiedNationalId,
            Guid patientId,
            CancellationToken cancellationToken)
        {
            Assert.True(unitOfWork.TransactionStarted);
            Assert.False(unitOfWork.Committed);
            return Task.FromResult(belongsToAnotherPatient);
        }

        public Task<bool> VerifiedNationalIdExistsAsync(
            string verifiedNationalId,
            CancellationToken cancellationToken) =>
            Task.FromResult(belongsToAnotherPatient);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool TransactionStarted { get; private set; }
        public bool SaveCalled { get; private set; }
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }
        public Exception? SaveException { get; init; }

        public Task<int> SaveChanges() => SaveChanges(CancellationToken.None);

        public Task<int> SaveChanges(CancellationToken cancellationToken)
        {
            Assert.True(TransactionStarted);
            SaveCalled = true;
            if (SaveException is not null)
            {
                throw SaveException;
            }

            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken)
        {
            TransactionStarted = true;
            return Task.CompletedTask;
        }

        public Task CommitTransactionAsync()
        {
            Assert.True(TransactionStarted);
            Committed = true;
            return Task.CompletedTask;
        }

        public Task RollBackTransactionAsync()
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
