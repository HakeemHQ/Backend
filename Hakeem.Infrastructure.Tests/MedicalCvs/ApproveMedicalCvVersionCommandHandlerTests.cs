using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.Commands.ApproveMedicalCvVersion;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces;
using Hakeem.Infrastructure.Tests.Fakes;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class ApproveMedicalCvVersionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOwnedVersionIsDraft_ApprovesAndPersistsIt()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var version = new MedicalCvVersion
        {
            Id = Guid.NewGuid(),
            VersionNumber = 3,
            Status = MedicalCvVersionStatus.Draft
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(userId, patient, version, unitOfWork);

        var result = await handler.Handle(
            new ApproveMedicalCvVersionCommand(version.Id),
            CancellationToken.None);

        Assert.Equal(version.Id, result.MedicalCvVersionId);
        Assert.Equal(3, result.VersionNumber);
        Assert.Equal(MedicalCvVersionStatus.Approved, result.Status);
        Assert.Equal(MedicalCvVersionStatus.Approved, version.Status);
        Assert.NotNull(version.ApprovedAt);
        Assert.Equal(version.ApprovedAt, result.ApprovedAt);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(MedicalCvVersionStatus.Queued)]
    [InlineData(MedicalCvVersionStatus.Processing)]
    [InlineData(MedicalCvVersionStatus.Failed)]
    [InlineData(MedicalCvVersionStatus.Approved)]
    public async Task Handle_WhenVersionIsNotDraft_ReturnsConflict(
        MedicalCvVersionStatus status)
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var version = new MedicalCvVersion
        {
            Id = Guid.NewGuid(),
            Status = status
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(userId, patient, version, unitOfWork);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new ApproveMedicalCvVersionCommand(version.Id),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.MedicalCvVersionNotDraft, exception.ErrorCode);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_WhenVersionIsMissingOrNotOwned_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(userId, patient, version: null, unitOfWork);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ApproveMedicalCvVersionCommand(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.MedicalCvNotFound, exception.ErrorCode);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static ApproveMedicalCvVersionCommandHandler CreateHandler(
        Guid userId,
        PatientProfile patient,
        MedicalCvVersion? version,
        IUnitOfWork unitOfWork)
    {
        if (version is not null)
        {
            version.MedicalCv = new MedicalCv { PatientId = patient.Id };
        }

        return new(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            new NullDoctorProfileRepository(),
            new NullDoctorPatientAccessRepository(),
            new FakeMedicalCvRepository(version, patient.Id),
            new FakeAuditLogRepository(),
            unitOfWork);
    }

    private sealed record FakeCurrentUserContext(Guid UserId)
        : ICurrentUserContext;

    private sealed class FakePatientProfileRepository(PatientProfile patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(null);

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

    private sealed class FakeMedicalCvRepository(
        MedicalCvVersion? version,
        Guid ownerPatientId)
        : IMedicalCvRepository
    {
        public Task<MedicalCvVersion?> GetVersionForPatientAsync(
            Guid medicalCvVersionId,
            Guid patientId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                version?.Id == medicalCvVersionId && patientId == ownerPatientId
                    ? version
                    : null);

        public Task<MedicalCv?> GetByLogicalIdentityAsync(
            Guid patientId,
            MedicalCvScopeType scopeType,
            string? focus,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> GetNextVersionNumberAsync(
            Guid medicalCvId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MedicalCvVersion?> GetVersionForGenerationAsync(
            Guid medicalCvVersionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(MedicalCv medicalCv) => throw new NotSupportedException();
        public void AddVersion(MedicalCvVersion version) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MedicalCv>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IMedicalCvRepository.MedicalCvVersionReadModel?> GetVersionByIdAsync(Guid medicalCvVersionId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<MedicalCvReadModel?> GetByIdAsync(Guid medicalCvId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<MedicalCvVersion?> GetVersionForApprovalAsync(Guid medicalCvVersionId, CancellationToken cancellationToken) =>
            Task.FromResult<MedicalCvVersion?>(version?.Id == medicalCvVersionId ? version : null);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChanges() => SaveChanges(CancellationToken.None);

        public Task<int> SaveChanges(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task CommitTransactionAsync() => throw new NotSupportedException();
        public Task RollBackTransactionAsync() => throw new NotSupportedException();
        public void Dispose()
        {
        }
    }
}
