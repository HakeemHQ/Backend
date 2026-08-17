using Hakeem.Application.Common;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Services.Access;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class DoctorPatientAccessExpirationServiceTests
{
    [Fact]
    public async Task ExpireForPair_UpdatesAccessThenRequestInOneTransaction()
    {
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;
        var repository = new ExpirationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new DoctorPatientAccessExpirationService(
            repository,
            unitOfWork);

        await service.ExpireForPairAsync(
            doctorId,
            patientId,
            utcNow,
            CancellationToken.None);

        Assert.Equal(["access", "request"], repository.CallOrder);
        Assert.Equal(doctorId, repository.DoctorProfileId);
        Assert.Equal(patientId, repository.PatientProfileId);
        Assert.Equal(utcNow, repository.UtcNow);
        Assert.Equal(1, unitOfWork.BeginTransactionCallCount);
        Assert.Equal(1, unitOfWork.CommitTransactionCallCount);
        Assert.Equal(0, unitOfWork.RollbackTransactionCallCount);
    }

    [Fact]
    public async Task ExpireForPatient_RequestUpdateFails_RollsBackTransaction()
    {
        var repository = new ExpirationRepository
        {
            RequestException = new IOException("Database update failed.")
        };
        var unitOfWork = new FakeUnitOfWork();
        var service = new DoctorPatientAccessExpirationService(
            repository,
            unitOfWork);

        await Assert.ThrowsAsync<IOException>(() =>
            service.ExpireForPatientAsync(
                Guid.NewGuid(),
                DateTime.UtcNow,
                CancellationToken.None));

        Assert.Equal(0, unitOfWork.CommitTransactionCallCount);
        Assert.Equal(1, unitOfWork.RollbackTransactionCallCount);
    }

    private sealed class ExpirationRepository
        : IDoctorPatientAccessRepository
    {
        public List<string> CallOrder { get; } = [];
        public Guid? DoctorProfileId { get; private set; }
        public Guid? PatientProfileId { get; private set; }
        public DateTime? UtcNow { get; private set; }
        public Exception? RequestException { get; init; }

        public Task<int> ExpireActiveAccessesAsync(
            Guid? doctorProfileId,
            Guid? patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            CallOrder.Add("access");
            Capture(doctorProfileId, patientProfileId, utcNow);
            return Task.FromResult(1);
        }

        public Task<int> ExpireRedeemedRequestsForExpiredAccessesAsync(
            Guid? doctorProfileId,
            Guid? patientProfileId,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            CallOrder.Add("request");
            Capture(doctorProfileId, patientProfileId, utcNow);
            return RequestException is null
                ? Task.FromResult(1)
                : Task.FromException<int>(RequestException);
        }

        private void Capture(
            Guid? doctorProfileId,
            Guid? patientProfileId,
            DateTime utcNow)
        {
            DoctorProfileId = doctorProfileId;
            PatientProfileId = patientProfileId;
            UtcNow = utcNow;
        }

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
}
