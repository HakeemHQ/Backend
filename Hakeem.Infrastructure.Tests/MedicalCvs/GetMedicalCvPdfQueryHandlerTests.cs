using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class GetMedicalCvPdfQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenVersionBelongsToPatient_OpensProtectedFile()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var medicalCvId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var version = new MedicalCvVersion
        {
            Id = versionId,
            MedicalCvId = medicalCvId,
            Status = MedicalCvVersionStatus.Draft,
            PdfFileKey = $"medical-cvs/{medicalCvId}/version-1.pdf"
        };
        var storage = new FakeFileStorage();
        var handler = new GetMedicalCvPdfQueryHandler(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            new FakeMedicalCvRepository(version, patient.Id),
            storage);

        var result = await handler.Handle(
            new GetMedicalCvPdfQuery(versionId),
            CancellationToken.None);

        Assert.NotNull(result.Content);
        Assert.Equal(version.PdfFileKey, storage.OpenedFileKey);
        await result.Content.DisposeAsync();
    }

    [Fact]
    public async Task Handle_WhenVersionDoesNotBelongToPatient_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var storage = new FakeFileStorage();
        var handler = new GetMedicalCvPdfQueryHandler(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            new FakeMedicalCvRepository(version: null, patient.Id),
            storage);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new GetMedicalCvPdfQuery(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.MedicalCvNotFound, exception.ErrorCode);
        Assert.Null(storage.OpenedFileKey);
    }

    [Fact]
    public async Task Handle_WhenVersionIsQueued_ReturnsConflictWithoutOpeningFile()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile { Id = Guid.NewGuid(), UserId = userId };
        var medicalCvId = Guid.NewGuid();
        var version = new MedicalCvVersion
        {
            Id = Guid.NewGuid(),
            MedicalCvId = medicalCvId,
            Status = MedicalCvVersionStatus.Queued
        };
        var storage = new FakeFileStorage();
        var handler = new GetMedicalCvPdfQueryHandler(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            new FakeMedicalCvRepository(version, patient.Id),
            storage);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new GetMedicalCvPdfQuery(version.Id),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.MedicalCvNotReady, exception.ErrorCode);
        Assert.Null(storage.OpenedFileKey);
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
            Task.FromResult<PatientProfile?>(
                patient.UserId == userId ? patient : null);

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

        public Task<MedicalCvVersion?> GetVersionForPatientAsync(
            Guid medicalCvVersionId,
            Guid patientId,
            CancellationToken cancellationToken)
        {
            var matches = version is not null &&
                          version.Id == medicalCvVersionId &&
                          patientId == ownerPatientId;

            return Task.FromResult(matches ? version : null);
        }

        public Task<MedicalCvVersion?> GetVersionForGenerationAsync(
            Guid medicalCvVersionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(MedicalCv medicalCv) => throw new NotSupportedException();
        public void AddVersion(MedicalCvVersion version) =>
            throw new NotSupportedException();
    }

    private sealed class FakeFileStorage : IMedicalCvFileStorage
    {
        public string? OpenedFileKey { get; private set; }

        public Task<string> SaveAsync(
            byte[] pdfBytes,
            Guid medicalCvId,
            int versionNumber,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            string fileKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(
            string fileKey,
            CancellationToken cancellationToken = default)
        {
            OpenedFileKey = fileKey;
            Stream stream = new MemoryStream("%PDF-1.7"u8.ToArray());
            return Task.FromResult(stream);
        }
    }
}
