using Hakeem.Application.Common;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvById;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class GetMedicalCvByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenMedicalCvBelongsToPatient_ReturnsVersionHistory()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var medicalCvId = Guid.NewGuid();
        var latestVersionId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 7, 19, 13, 20, 0, DateTimeKind.Utc);
        var repository = new FakeMedicalCvReadRepository(
            new MedicalCvDetailReadModel(
                medicalCvId,
                "Diabetes Medical CV",
                MedicalCvScopeType.Focused,
                "Diabetes",
                createdAt,
                createdAt.AddDays(1),
                [
                    new MedicalCvVersionDetailReadModel(
                        latestVersionId,
                        2,
                        MedicalCvVersionStatus.Approved,
                        createdAt.AddDays(1),
                        createdAt.AddDays(1).AddHours(1),
                        true),
                    new MedicalCvVersionDetailReadModel(
                        Guid.NewGuid(),
                        1,
                        MedicalCvVersionStatus.Draft,
                        createdAt,
                        null,
                        true)
                ]));
        var handler = new GetMedicalCvByIdQueryHandler(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            repository);

        var result = await handler.Handle(
            new GetMedicalCvByIdQuery(medicalCvId),
            CancellationToken.None);

        Assert.Equal(medicalCvId, result.MedicalCvId);
        Assert.Equal(MedicalCvScopeType.Focused, result.ScopeType);
        Assert.Equal("Diabetes", result.Focus);
        Assert.Equal(2, result.Versions.Count);
        Assert.Equal(latestVersionId, result.Versions[0].MedicalCvVersionId);
        Assert.Equal(2, result.Versions[0].VersionNumber);
        Assert.True(result.Versions[0].PdfAvailable);
        Assert.Equal(patient.Id, repository.PatientId);
    }

    [Fact]
    public async Task Handle_WhenMedicalCvIsMissingOrNotOwned_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var repository = new FakeMedicalCvReadRepository(detail: null);
        var handler = new GetMedicalCvByIdQueryHandler(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            repository);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new GetMedicalCvByIdQuery(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.MedicalCvNotFound, exception.ErrorCode);
        Assert.Equal(patient.Id, repository.PatientId);
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

    private sealed class FakeMedicalCvReadRepository(MedicalCvDetailReadModel? detail)
        : IMedicalCvReadRepository
    {
        public Guid? PatientId { get; private set; }

        public Task<MedicalCvDetailReadModel?> GetDetailForPatientAsync(
            Guid medicalCvId,
            Guid patientId,
            CancellationToken cancellationToken)
        {
            PatientId = patientId;
            return Task.FromResult(
                detail?.MedicalCvId == medicalCvId ? detail : null);
        }

        public Task<PaginatedResult<MedicalCvListReadModel>> GetForPatientAsync(
            Guid patientId,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
