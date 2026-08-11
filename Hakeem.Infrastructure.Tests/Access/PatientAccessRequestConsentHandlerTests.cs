using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.PatientAccessRequests.Commands.ApprovePatientAccessRequest;
using Hakeem.Application.Features.PatientAccessRequests.Commands.RejectPatientAccessRequest;
using Hakeem.Application.Features.PatientAccessRequests.Queries.GetPatientAccessRequests;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class PatientAccessRequestConsentHandlerTests
{
    [Fact]
    public async Task Get_ReturnsOwnRequestsWithDoctorDetails()
    {
        var patient = CreatePatient();
        var doctor = new DoctorProfile
        {
            Id = Guid.NewGuid(),
            Specialty = "Cardiology",
            User = new User
            {
                FirstName = "Dr. Ahmed",
                LastName = "Hassan"
            }
        };
        var accessRequest = CreateAccessRequest(patient.Id);
        accessRequest.Doctor = doctor;
        var repository = new FakeAccessRequestRepository(accessRequest);
        var handler = new GetPatientAccessRequestsQueryHandler(
            new FakePatientProfileRepository(patient),
            repository,
            new FakeCurrentUserContext(patient.UserId));

        var result = await handler.Handle(
            new GetPatientAccessRequestsQuery(
                PatientAccessRequestStatus.Pending,
                PageNumber: 2,
                PageSize: 5),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(accessRequest.Id, item.RequestId);
        Assert.Equal(doctor.Id, item.Doctor.DoctorId);
        Assert.Equal("Dr. Ahmed Hassan", item.Doctor.FullName);
        Assert.Equal("Cardiology", item.Doctor.Specialty);
        Assert.Equal("Pending", item.Status);
        Assert.Equal(patient.Id, repository.QueriedPatientId);
        Assert.Equal(PatientAccessRequestStatus.Pending, repository.QueriedStatus);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Approve_AtomicallyStoresHashAndReturnsPlaintextCodeOnce()
    {
        var patient = CreatePatient();
        var accessRequest = CreateAccessRequest(patient.Id);
        var repository = new FakeAccessRequestRepository(accessRequest);
        var handler = new ApprovePatientAccessRequestCommandHandler(
            new FakePatientProfileRepository(patient),
            repository,
            new FakeAccessCodeService(),
            new FakeCurrentUserContext(patient.UserId),
            Options.Create(new PatientAccessConfiguration()));

        var result = await handler.Handle(
            new ApprovePatientAccessRequestCommand(accessRequest.Id),
            CancellationToken.None);

        Assert.Equal(accessRequest.Id, result.RequestId);
        Assert.Equal("Approved", result.Status);
        Assert.Equal("482913", result.OneTimeCode);
        Assert.Equal("bcrypt-hash", repository.StoredCodeHash);
        Assert.NotEqual(result.OneTimeCode, repository.StoredCodeHash);
        Assert.NotNull(repository.ApprovedAt);
        Assert.Equal(
            TimeSpan.FromMinutes(6),
            repository.CodeExpiresAt!.Value - repository.ApprovedAt.Value);
        Assert.Equal(repository.CodeExpiresAt, result.CodeExpiresAt);
    }

    [Fact]
    public async Task Approve_WhenRequestDoesNotBelongToPatient_ReturnsNotFound()
    {
        var patient = CreatePatient();
        var repository = new FakeAccessRequestRepository(request: null);
        var handler = new ApprovePatientAccessRequestCommandHandler(
            new FakePatientProfileRepository(patient),
            repository,
            new FakeAccessCodeService(),
            new FakeCurrentUserContext(patient.UserId),
            Options.Create(new PatientAccessConfiguration()));

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ApprovePatientAccessRequestCommand(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessRequestNotFound, exception.ErrorCode);
        Assert.Null(repository.StoredCodeHash);
    }

    [Fact]
    public async Task Approve_WhenAlreadyActedOn_ReturnsConflictWithoutGeneratingCode()
    {
        var patient = CreatePatient();
        var accessRequest = CreateAccessRequest(patient.Id);
        accessRequest.Status = PatientAccessRequestStatus.Rejected;
        var codeService = new FakeAccessCodeService();
        var handler = new ApprovePatientAccessRequestCommandHandler(
            new FakePatientProfileRepository(patient),
            new FakeAccessRequestRepository(accessRequest),
            codeService,
            new FakeCurrentUserContext(patient.UserId),
            Options.Create(new PatientAccessConfiguration()));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new ApprovePatientAccessRequestCommand(accessRequest.Id),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessRequestAlreadyActedOn, exception.ErrorCode);
        Assert.False(codeService.GenerateCalled);
    }

    [Fact]
    public async Task Approve_WhenConcurrentActionWins_ReturnsConflictAndDoesNotReturnCode()
    {
        var patient = CreatePatient();
        var accessRequest = CreateAccessRequest(patient.Id);
        var repository = new FakeAccessRequestRepository(accessRequest)
        {
            ApproveAffectedRows = 0
        };
        var handler = new ApprovePatientAccessRequestCommandHandler(
            new FakePatientProfileRepository(patient),
            repository,
            new FakeAccessCodeService(),
            new FakeCurrentUserContext(patient.UserId),
            Options.Create(new PatientAccessConfiguration()));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new ApprovePatientAccessRequestCommand(accessRequest.Id),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessRequestAlreadyActedOn, exception.ErrorCode);
    }

    [Fact]
    public async Task Reject_AtomicallyChangesPendingRequestToRejected()
    {
        var patient = CreatePatient();
        var accessRequest = CreateAccessRequest(patient.Id);
        var repository = new FakeAccessRequestRepository(accessRequest);
        var handler = new RejectPatientAccessRequestCommandHandler(
            new FakePatientProfileRepository(patient),
            repository,
            new FakeCurrentUserContext(patient.UserId));

        var result = await handler.Handle(
            new RejectPatientAccessRequestCommand(accessRequest.Id),
            CancellationToken.None);

        Assert.Equal(accessRequest.Id, result.RequestId);
        Assert.Equal("Rejected", result.Status);
        Assert.NotNull(repository.RejectedAt);
        Assert.Equal(DateTimeKind.Utc, repository.RejectedAt.Value.Kind);
        Assert.Null(repository.StoredCodeHash);
    }

    [Fact]
    public async Task Reject_WhenConcurrentActionWins_ReturnsConflict()
    {
        var patient = CreatePatient();
        var accessRequest = CreateAccessRequest(patient.Id);
        var repository = new FakeAccessRequestRepository(accessRequest)
        {
            RejectAffectedRows = 0
        };
        var handler = new RejectPatientAccessRequestCommandHandler(
            new FakePatientProfileRepository(patient),
            repository,
            new FakeCurrentUserContext(patient.UserId));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new RejectPatientAccessRequestCommand(accessRequest.Id),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.PatientAccessRequestAlreadyActedOn, exception.ErrorCode);
    }

    private static PatientProfile CreatePatient() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid()
    };

    private static PatientAccessRequest CreateAccessRequest(Guid patientId) => new()
    {
        Id = Guid.NewGuid(),
        PatientProfileId = patientId,
        DoctorProfileId = Guid.NewGuid(),
        Status = PatientAccessRequestStatus.Pending,
        RequestedAt = DateTime.UtcNow.AddMinutes(-1)
    };

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakeAccessCodeService : IOneTimeAccessCodeService
    {
        public bool GenerateCalled { get; private set; }

        public IssuedOneTimeAccessCode Generate()
        {
            GenerateCalled = true;
            return new IssuedOneTimeAccessCode("482913", "bcrypt-hash");
        }

        public bool Verify(string code, string codeHash) =>
            code == "482913" && codeHash == "bcrypt-hash";
    }

    private sealed class FakePatientProfileRepository(PatientProfile patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient.Id == patientProfileId ? patient : null);

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

    private sealed class FakeAccessRequestRepository(PatientAccessRequest? request)
        : IPatientAccessRequestRepository
    {
        public int ApproveAffectedRows { get; init; } = 1;
        public int RejectAffectedRows { get; init; } = 1;
        public Guid? QueriedPatientId { get; private set; }
        public PatientAccessRequestStatus? QueriedStatus { get; private set; }
        public int? QueriedPageNumber { get; private set; }
        public int? QueriedPageSize { get; private set; }
        public string? StoredCodeHash { get; private set; }
        public DateTime? ApprovedAt { get; private set; }
        public DateTime? CodeExpiresAt { get; private set; }
        public DateTime? RejectedAt { get; private set; }

        public Task<Hakeem.Application.Common.PaginatedResult<PatientAccessRequest>> GetForPatientAsync(
            Guid patientProfileId,
            PatientAccessRequestStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            QueriedPatientId = patientProfileId;
            QueriedStatus = status;
            QueriedPageNumber = pageNumber;
            QueriedPageSize = pageSize;
            IReadOnlyList<PatientAccessRequest> result = request is not null &&
                request.PatientProfileId == patientProfileId &&
                (!status.HasValue || request.Status == status.Value)
                    ? [request]
                    : [];
            return Task.FromResult(
                new Hakeem.Application.Common.PaginatedResult<PatientAccessRequest>(
                    result,
                    result.Count,
                    pageNumber,
                    pageSize));
        }

        public Task<PatientAccessRequest?> GetByIdForPatientAsync(
            Guid requestId,
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientAccessRequest?>(
                request is not null &&
                request.Id == requestId &&
                request.PatientProfileId == patientProfileId
                    ? request
                    : null);

        public Task<int> ApprovePendingAsync(
            Guid requestId,
            Guid patientProfileId,
            string codeHash,
            DateTime approvedAt,
            DateTime codeExpiresAt,
            CancellationToken cancellationToken)
        {
            StoredCodeHash = codeHash;
            ApprovedAt = approvedAt;
            CodeExpiresAt = codeExpiresAt;
            return Task.FromResult(ApproveAffectedRows);
        }

        public Task<int> RejectPendingAsync(
            Guid requestId,
            Guid patientProfileId,
            DateTime rejectedAt,
            CancellationToken cancellationToken)
        {
            RejectedAt = rejectedAt;
            return Task.FromResult(RejectAffectedRows);
        }

        public Task<PatientProfile?> GetPatientByCodeAsync(
            string patientCode,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> HasPendingRequestAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> HasActiveAccessAsync(
            Guid doctorProfileId,
            Guid patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(PatientAccessRequest accessRequest) =>
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
}
