using Hakeem.Application.Common;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class GetMedicalCvsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLatestVersionSummariesAndPagination()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var medicalCvId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var repository = new FakeMedicalCvReadRepository(
            new PaginatedResult<MedicalCvListReadModel>(
                [new MedicalCvListReadModel(
                    medicalCvId,
                    "Diabetes Medical CV",
                    MedicalCvScopeType.Focused,
                    "Diabetes",
                    new LatestMedicalCvVersionReadModel(
                        versionId,
                        3,
                        MedicalCvVersionStatus.Draft))],
                totalCount: 21,
                pageNumber: 2,
                pageSize: 10));
        var handler = new GetMedicalCvsQueryHandler(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            repository);

        var result = await handler.Handle(
            new GetMedicalCvsQuery(" diabetes ", Page: 2, PageSize: 10),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(medicalCvId, item.MedicalCvId);
        Assert.Equal(MedicalCvScopeType.Focused, item.ScopeType);
        Assert.Equal("Diabetes", item.Focus);
        Assert.NotNull(item.LatestVersion);
        Assert.Equal(versionId, item.LatestVersion.MedicalCvVersionId);
        Assert.Equal(3, item.LatestVersion.VersionNumber);
        Assert.Equal(MedicalCvVersionStatus.Draft, item.LatestVersion.Status);
        Assert.Equal(2, result.Pagination.Page);
        Assert.Equal(10, result.Pagination.PageSize);
        Assert.Equal(21, result.Pagination.TotalItems);
        Assert.Equal(3, result.Pagination.TotalPages);
        Assert.Equal(patient.Id, repository.PatientId);
        Assert.Equal(" diabetes ", repository.Search);
    }

    [Fact]
    public async Task Handle_WhenPatientProfileDoesNotExist_ReturnsUnauthorized()
    {
        var repository = new FakeMedicalCvReadRepository(
            new PaginatedResult<MedicalCvListReadModel>([], 0, 1, 20));
        var handler = new GetMedicalCvsQueryHandler(
            new FakeCurrentUserContext(Guid.NewGuid()),
            new FakePatientProfileRepository(patient: null),
            repository);

        var exception = await Assert.ThrowsAsync<UnAuthorizedException>(() =>
            handler.Handle(
                new GetMedicalCvsQuery(null),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.AuthUnauthorized, exception.ErrorCode);
        Assert.Null(repository.PatientId);
    }

    private sealed record FakeCurrentUserContext(Guid UserId)
        : ICurrentUserContext;

    private sealed class FakePatientProfileRepository(PatientProfile? patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(null);

        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(patient?.UserId == userId ? patient : null);

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

    private sealed class FakeMedicalCvReadRepository(
        PaginatedResult<MedicalCvListReadModel> result)
        : IMedicalCvReadRepository
    {
        public Guid? PatientId { get; private set; }
        public string? Search { get; private set; }

        public Task<PaginatedResult<MedicalCvListReadModel>> GetForPatientAsync(
            Guid patientId,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            PatientId = patientId;
            Search = search;
            return Task.FromResult(result);
        }

        public Task<MedicalCvDetailReadModel?> GetDetailForPatientAsync(
            Guid medicalCvId,
            Guid patientId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
