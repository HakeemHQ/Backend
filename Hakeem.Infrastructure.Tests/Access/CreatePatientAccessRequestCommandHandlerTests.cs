using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.PatientAccessRequests.Commands.CreatePatientAccessRequest;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class CreatePatientAccessRequestCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesPendingRequestTransactionally()
    {
        var doctor = CreateDoctor(AccountStatus.Active);
        var patient = CreatePatient(IdentityVerificationStatus.Verified);
        var repository = new FakePatientAccessRequestRepository(patient);
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(doctor, repository, unitOfWork);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(unitOfWork.TransactionStarted);
        Assert.True(unitOfWork.SaveCalled);
        Assert.True(unitOfWork.Committed);
        Assert.False(unitOfWork.RolledBack);
        Assert.NotNull(repository.AddedRequest);
        Assert.Equal(doctor.Id, repository.AddedRequest.DoctorProfileId);
        Assert.Equal(patient.Id, repository.AddedRequest.PatientProfileId);
        Assert.Equal(PatientAccessRequestStatus.Pending, repository.AddedRequest.Status);
        Assert.Null(repository.AddedRequest.CodeHash);
        Assert.Null(repository.AddedRequest.Access);
        Assert.Equal(repository.AddedRequest.Id, result.RequestId);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(DateTimeKind.Utc, result.RequestedAt.Kind);
    }

    [Fact]
    public async Task Handle_WhenDoctorIsSuspended_ReturnsUnauthorizedWithoutTransaction()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            CreateDoctor(AccountStatus.Suspended),
            new FakePatientAccessRequestRepository(
                CreatePatient(IdentityVerificationStatus.Verified)),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<UnAuthorizedException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.AuthAccountInactive, exception.ErrorCode);
        Assert.False(unitOfWork.TransactionStarted);
    }

    [Fact]
    public async Task Handle_WhenPatientDoesNotExist_ReturnsNotFound()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            CreateDoctor(AccountStatus.Active),
            new FakePatientAccessRequestRepository(patient: null),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessPatientNotFound, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
    }

    [Fact]
    public async Task Handle_WhenPatientIsNotVerified_ReturnsUnprocessable()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            CreateDoctor(AccountStatus.Active),
            new FakePatientAccessRequestRepository(
                CreatePatient(IdentityVerificationStatus.Pending)),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessPatientNotVerified, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
    }

    [Fact]
    public async Task Handle_WhenPendingRequestExists_ReturnsConflict()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repository = new FakePatientAccessRequestRepository(
            CreatePatient(IdentityVerificationStatus.Verified))
        {
            HasPendingRequest = true
        };
        var handler = CreateHandler(
            CreateDoctor(AccountStatus.Active),
            repository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessPendingRequestExists, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
        Assert.Null(repository.AddedRequest);
    }

    [Fact]
    public async Task Handle_WhenActiveAccessExists_ReturnsConflict()
    {
        var unitOfWork = new FakeUnitOfWork();
        var repository = new FakePatientAccessRequestRepository(
            CreatePatient(IdentityVerificationStatus.Verified))
        {
            HasActiveAccess = true
        };
        var handler = CreateHandler(
            CreateDoctor(AccountStatus.Active),
            repository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessActiveAccessExists, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
        Assert.Null(repository.AddedRequest);
    }

    [Fact]
    public async Task Handle_WhenUniquePendingIndexWinsRace_ReturnsConflict()
    {
        var unitOfWork = new FakeUnitOfWork
        {
            SaveException = new DbUpdateException(
                "Violation of unique index IX_PatientAccessRequests_DoctorProfileId_PatientProfileId")
        };
        var handler = CreateHandler(
            CreateDoctor(AccountStatus.Active),
            new FakePatientAccessRequestRepository(
                CreatePatient(IdentityVerificationStatus.Verified)),
            unitOfWork);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessPendingRequestExists, exception.ErrorCode);
        Assert.True(unitOfWork.RolledBack);
        Assert.False(unitOfWork.Committed);
    }

    private static CreatePatientAccessRequestCommandHandler CreateHandler(
        DoctorProfile doctor,
        FakePatientAccessRequestRepository repository,
        FakeUnitOfWork unitOfWork)
    {
        return new CreatePatientAccessRequestCommandHandler(
            repository,
            new FakeDoctorProfileRepository(doctor),
            new FakeCurrentUserContext(doctor.UserId),
            unitOfWork);
    }

    private static CreatePatientAccessRequestCommand CreateCommand() => new()
    {
        PatientCode = "h89k-27p"
    };

    private static DoctorProfile CreateDoctor(AccountStatus status)
    {
        var userId = Guid.NewGuid();
        return new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = new User
            {
                Id = userId,
                Role = ApplicationRole.Doctor,
                Status = status
            }
        };
    }

    private static PatientProfile CreatePatient(IdentityVerificationStatus status) => new()
    {
        Id = Guid.NewGuid(),
        PatientCode = "H89K-27P",
        IdentityVerificationStatus = status
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

    private sealed class FakePatientAccessRequestRepository(PatientProfile? patient)
        : IPatientAccessRequestRepository
    {
        public bool HasPendingRequest { get; init; }
        public bool HasActiveAccess { get; init; }
        public PatientAccessRequest? AddedRequest { get; private set; }

        public Task<PatientProfile?> GetPatientByCodeAsync(
            string patientCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(patient?.PatientCode == patientCode ? patient : null);

        public Task<bool> HasPendingRequestAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult(HasPendingRequest);

        public Task<bool> HasActiveAccessAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            Task.FromResult(HasActiveAccess);

        public void Add(PatientAccessRequest accessRequest)
        {
            AddedRequest = accessRequest;
        }

        public Task<IReadOnlyList<PatientAccessRequest>> GetForPatientAsync(
            Guid patientProfileId,
            PatientAccessRequestStatus? status,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PatientAccessRequest?> GetByIdForPatientAsync(
            Guid requestId,
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> ApprovePendingAsync(
            Guid requestId,
            Guid patientProfileId,
            string codeHash,
            DateTime approvedAt,
            DateTime codeExpiresAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> RejectPendingAsync(
            Guid requestId,
            Guid patientProfileId,
            DateTime rejectedAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PatientAccessRequest>> GetCodeCandidatesAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> ExpireStaleActiveAccessAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> RedeemApprovedAsync(
            Guid requestId,
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime redeemedAt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void AddAccess(DoctorPatientAccess access) =>
            throw new NotSupportedException();
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
