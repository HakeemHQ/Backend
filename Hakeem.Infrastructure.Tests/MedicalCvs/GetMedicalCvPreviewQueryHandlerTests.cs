using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPreview;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class GetMedicalCvPreviewQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithValidToken_OpensPdf()
    {
        var patientId = Guid.NewGuid();
        var medicalCvId = Guid.NewGuid();
        var version = new MedicalCvVersion
        {
            Id = Guid.NewGuid(),
            MedicalCvId = medicalCvId,
            Status = MedicalCvVersionStatus.Draft,
            PdfFileKey = $"medical-cvs/{medicalCvId}/version-1.pdf"
        };
        var storage = new FakeFileStorage();
        var handler = new GetMedicalCvPreviewQueryHandler(
            new FakePreviewLinkService(patientId, isValid: true),
            new FakeMedicalCvRepository(version, patientId),
            storage);

        var result = await handler.Handle(
            new GetMedicalCvPreviewQuery(
                version.Id,
                "valid-token"),
            CancellationToken.None);

        Assert.Equal(version.PdfFileKey, storage.OpenedFileKey);
        await result.Content.DisposeAsync();
    }

    [Fact]
    public async Task Handle_WithInvalidOrExpiredToken_ReturnsUnauthorized()
    {
        var handler = new GetMedicalCvPreviewQueryHandler(
            new FakePreviewLinkService(Guid.NewGuid(), isValid: false),
            new FakeMedicalCvRepository(null, Guid.NewGuid()),
            new FakeFileStorage());

        var exception = await Assert.ThrowsAsync<UnAuthorizedException>(() =>
            handler.Handle(
                new GetMedicalCvPreviewQuery(
                    Guid.NewGuid(),
                    "invalid-token"),
                CancellationToken.None));

        Assert.Equal(
            ErrorCodes.MedicalCvPreviewInvalidOrExpired,
            exception.ErrorCode);
    }

    private sealed class FakePreviewLinkService(
        Guid patientId,
        bool isValid)
        : IMedicalCvPreviewLinkService
    {
        public MedicalCvPreviewLink Create(
            Guid patientId,
            Guid medicalCvId,
            Guid medicalCvVersionId) =>
            throw new NotSupportedException();

        public bool TryValidate(
            string token,
            Guid medicalCvVersionId,
            out MedicalCvPreviewAccess access)
        {
            access = new MedicalCvPreviewAccess(
                patientId,
                Guid.NewGuid(),
                medicalCvVersionId);
            return isValid;
        }
    }

    private sealed class FakeMedicalCvRepository(
        MedicalCvVersion? version,
        Guid ownerPatientId)
        : IMedicalCvRepository
    {
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
            throw new NotSupportedException();
    }

    private sealed class FakeFileStorage : IMedicalCvFileStorage
    {
        public string? OpenedFileKey { get; private set; }

        public Task<Stream> OpenReadAsync(
            string fileKey,
            CancellationToken cancellationToken = default)
        {
            OpenedFileKey = fileKey;
            Stream content = new MemoryStream("%PDF-1.7"u8.ToArray());
            return Task.FromResult(content);
        }

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
    }
}
