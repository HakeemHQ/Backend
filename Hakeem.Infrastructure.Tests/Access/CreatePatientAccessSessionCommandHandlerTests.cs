using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.PatientAccessSessions.Commands.CreatePatientAccessSession;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class CreatePatientAccessSessionCommandHandlerTests
{
    [Fact]
    public async Task Redeem_ValidBoundCode_AtomicallyCreatesConfiguredAccess()
    {
        var fixture = CreateFixture();

        var result = await fixture.Handler.Handle(Command(), CancellationToken.None);

        Assert.True(fixture.UnitOfWork.TransactionStarted);
        Assert.True(fixture.UnitOfWork.Committed);
        Assert.False(fixture.UnitOfWork.RolledBack);
        Assert.Equal(1, fixture.Repository.RedeemCalls);
        Assert.NotNull(fixture.Repository.AddedAccess);
        Assert.Equal(fixture.Request.Id, fixture.Repository.AddedAccess.PatientAccessRequestId);
        Assert.Equal(fixture.Doctor.Id, fixture.Repository.AddedAccess.DoctorProfileId);
        Assert.Equal(fixture.Patient.Id, fixture.Repository.AddedAccess.PatientProfileId);
        Assert.Equal(DoctorPatientAccessStatus.Active, fixture.Repository.AddedAccess.Status);
        Assert.Equal(TimeSpan.FromMinutes(90), result.ExpiresAt - result.GrantedAt);
        Assert.Equal("2000-05-12", result.Patient.BirthDate);
    }

    [Fact]
    public async Task Redeem_CodeBoundToDifferentDoctor_ReturnsInvalidCode()
    {
        var fixture = CreateFixture();
        fixture.Repository.CodeCandidates = [];

        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            fixture.Handler.Handle(Command(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessInvalidCode, exception.ErrorCode);
        Assert.False(fixture.UnitOfWork.TransactionStarted);
    }

    [Fact]
    public async Task Redeem_ExpiredCode_ReturnsUnprocessableEntity()
    {
        var fixture = CreateFixture();
        fixture.Request.CodeExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            fixture.Handler.Handle(Command(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessCodeExpired, exception.ErrorCode);
        Assert.Equal(1, fixture.Repository.ExpireApprovedCalls);
        Assert.Equal(PatientAccessRequestStatus.Expired, fixture.Request.Status);
        Assert.Null(fixture.Repository.AddedAccess);
    }

    [Fact]
    public async Task Redeem_AlreadyRedeemedCode_ReturnsConflict()
    {
        var fixture = CreateFixture();
        fixture.Request.Status = PatientAccessRequestStatus.Redeemed;

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Handler.Handle(Command(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessCodeAlreadyRedeemed, exception.ErrorCode);
        Assert.False(fixture.UnitOfWork.TransactionStarted);
    }

    [Fact]
    public async Task Redeem_WhenConcurrentRedemptionWins_ReturnsConflictAndRollsBack()
    {
        var fixture = CreateFixture();
        fixture.Repository.RedeemAffectedRows = 0;

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Handler.Handle(Command(), CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessCodeAlreadyRedeemed, exception.ErrorCode);
        Assert.True(fixture.UnitOfWork.RolledBack);
        Assert.False(fixture.UnitOfWork.Committed);
        Assert.Null(fixture.Repository.AddedAccess);
    }

    private static Fixture CreateFixture()
    {
        var doctor = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Status = AccountStatus.Active }
        };
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            PatientCode = "H89K-27P",
            FullName = "Mazen Mohamed",
            BirthDate = new DateTime(2000, 5, 12)
        };
        var accessRequest = new PatientAccessRequest
        {
            Id = Guid.NewGuid(),
            DoctorProfileId = doctor.Id,
            PatientProfileId = patient.Id,
            Patient = patient,
            Status = PatientAccessRequestStatus.Approved,
            CodeHash = "hash-for-482913",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        var repository = new FakeAccessRequestRepository(patient, [accessRequest]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreatePatientAccessSessionCommandHandler(
            repository,
            new FakeDoctorProfileRepository(doctor),
            new FakeAccessCodeService(),
            new FakeCurrentUserContext(doctor.UserId),
            unitOfWork,
            Options.Create(new PatientAccessConfiguration
            {
                CodeLifetimeMinutes = 6,
                AccessLifetimeMinutes = 90
            }));

        return new Fixture(
            handler,
            repository,
            unitOfWork,
            doctor,
            patient,
            accessRequest);
    }

    private static CreatePatientAccessSessionCommand Command() => new()
    {
        PatientCode = "h89k-27p",
        OneTimeCode = "482913"
    };

    private sealed record Fixture(
        CreatePatientAccessSessionCommandHandler Handler,
        FakeAccessRequestRepository Repository,
        FakeUnitOfWork UnitOfWork,
        DoctorProfile Doctor,
        PatientProfile Patient,
        PatientAccessRequest Request);

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakeAccessCodeService : IOneTimeAccessCodeService
    {
        public IssuedOneTimeAccessCode Generate() => throw new NotSupportedException();

        public bool Verify(string code, string codeHash) =>
            codeHash == $"hash-for-{code}";
    }

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

    private sealed class FakeAccessRequestRepository(
        PatientProfile patient,
        IReadOnlyList<PatientAccessRequest> codeCandidates)
        : IPatientAccessRequestRepository
    {
        public IReadOnlyList<PatientAccessRequest> CodeCandidates { get; set; }
            = codeCandidates;
        public int RedeemAffectedRows { get; set; } = 1;
        public int RedeemCalls { get; private set; }
        public int ExpireApprovedCalls { get; private set; }
        public bool HasActiveAccess { get; set; }
        public DoctorPatientAccess? AddedAccess { get; private set; }

        public Task<PatientProfile?> GetPatientByCodeAsync(
            string patientCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient.PatientCode == patientCode ? patient : null);

        public Task<IReadOnlyList<PatientAccessRequest>> GetCodeCandidatesAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CodeCandidates);

        public Task<int> ExpireStaleActiveAccessAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<bool> HasActiveAccessAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            Task.FromResult(HasActiveAccess);

        public Task<int> RedeemApprovedAsync(
            Guid requestId,
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime redeemedAt,
            CancellationToken cancellationToken)
        {
            RedeemCalls++;
            return Task.FromResult(RedeemAffectedRows);
        }

        public Task<int> ExpireApprovedAsync(
            Guid requestId,
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            ExpireApprovedCalls++;
            var request = CodeCandidates.Single(candidate => candidate.Id == requestId);
            if (request.Status != PatientAccessRequestStatus.Approved ||
                request.CodeExpiresAt > utcNow)
            {
                return Task.FromResult(0);
            }

            request.Status = PatientAccessRequestStatus.Expired;
            return Task.FromResult(1);
        }

        public void AddAccess(DoctorPatientAccess access) => AddedAccess = access;

        public Task<int> ExpireStaleRequestsAsync(
            Guid patientProfileId,
            DateTime pendingExpiresBefore,
            DateTime utcNow,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasBlockingRequestAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime pendingExpiresBefore,
            DateTime utcNow,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Add(PatientAccessRequest accessRequest) => throw new NotSupportedException();
        public Task<Hakeem.Application.Common.PaginatedResult<PatientAccessRequest>> GetForPatientAsync(
            Guid patientProfileId,
            PatientAccessRequestStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PatientAccessRequest?> GetByIdForPatientAsync(
            Guid requestId,
            Guid patientProfileId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> ApprovePendingAsync(
            Guid requestId,
            Guid patientProfileId,
            string codeHash,
            DateTime approvedAt,
            DateTime codeExpiresAt,
            DateTime pendingExpiresBefore,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> RejectPendingAsync(
            Guid requestId,
            Guid patientProfileId,
            DateTime rejectedAt,
            DateTime pendingExpiresBefore,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool TransactionStarted { get; private set; }
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }

        public Task<int> SaveChanges() => SaveChanges(CancellationToken.None);
        public Task<int> SaveChanges(CancellationToken cancellationToken) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken)
        {
            TransactionStarted = true;
            return Task.CompletedTask;
        }
        public Task CommitTransactionAsync()
        {
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
