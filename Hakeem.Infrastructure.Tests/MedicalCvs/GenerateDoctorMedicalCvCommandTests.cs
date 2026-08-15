using Hakeem.Application.Features.Doctor.MedicalCvs.Commands;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.MedicalCvs;
using Microsoft.Extensions.Localization;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class GenerateDoctorMedicalCvCommandTests
{
    [Fact]
    public async Task Handler_ReturnsQueuedVersionContractForDoctor()
    {
        var patientId = Guid.NewGuid();
        var result = new MedicalCvGenerationResult(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cardiology Medical CV",
            1,
            MedicalCvScopeType.Full,
            null,
            string.Empty,
            MedicalCvVersionStatus.Queued,
            DateTime.UtcNow,
            Guid.NewGuid(),
            MedicalCvCreatedByRole.Doctor);
        var generationService = new FakeGenerationService(result);
        var previewExpiresAt = new DateTimeOffset(
            2026, 8, 15, 18, 25, 15, TimeSpan.Zero);
        var previewLinkService = new FakePreviewLinkService(previewExpiresAt);
        var handler = new GenerateDoctorMedicalCvCommandHandler(
            generationService,
            previewLinkService,
            new FakeFileUrlResolver());

        var response = await handler.Handle(
            new GenerateDoctorMedicalCvCommand(
                patientId,
                "Cardiology Medical CV"),
            CancellationToken.None);

        Assert.Equal(result.MedicalCvId, response.MedicalCvId);
        Assert.Equal("Cardiology Medical CV", response.Title);
        Assert.Equal(
            result.MedicalCvVersionId,
            response.LatestVersion.MedicalCvVersionId);
        Assert.Equal(1, response.LatestVersion.VersionNumber);
        Assert.Equal("Queued", response.LatestVersion.GenerationStatus);
        Assert.Equal("Unreviewed", response.LatestVersion.VerificationStatus);
        Assert.Equal("Doctor", response.LatestVersion.CreatedByRole);
        Assert.Equal(
            $"https://api.test/medical-cv-versions/" +
            $"{result.MedicalCvVersionId}/preview?token=token%2B%2F%3D",
            response.LatestVersion.PdfUrl);
        Assert.Equal(previewExpiresAt, response.LatestVersion.PreviewExpiresAt);
        Assert.Equal(patientId, previewLinkService.PatientId);
        Assert.Equal(result.MedicalCvId, previewLinkService.MedicalCvId);
        Assert.Equal(result.MedicalCvVersionId, previewLinkService.VersionId);
        Assert.Equal(patientId, generationService.PatientId);
        Assert.Equal(
            MedicalCvCreatedByRole.Doctor,
            generationService.CreatedByRole);
    }

    [Fact]
    public async Task Validator_RejectsMissingAndOversizedTitles()
    {
        var validator = new GenerateDoctorMedicalCvCommandValidator(
            new PassThroughLocalizer());

        var missingResult = await validator.ValidateAsync(
            new GenerateDoctorMedicalCvCommand(Guid.NewGuid(), string.Empty));
        var oversizedResult = await validator.ValidateAsync(
            new GenerateDoctorMedicalCvCommand(
                Guid.NewGuid(),
                new string('x', 201)));

        Assert.False(missingResult.IsValid);
        Assert.False(oversizedResult.IsValid);
    }

    private sealed class FakeGenerationService(MedicalCvGenerationResult result)
        : IMedicalCvGenerationService
    {
        public Guid? PatientId { get; private set; }
        public MedicalCvCreatedByRole? CreatedByRole { get; private set; }

        public Task<MedicalCvGenerationResult> GenerateFullAsync(
            Guid patientId,
            string title,
            string language,
            MedicalCvCreatedByRole createdByRole,
            CancellationToken cancellationToken = default)
        {
            PatientId = patientId;
            CreatedByRole = createdByRole;
            return Task.FromResult(result);
        }

        public Task<MedicalCvGenerationResult> GenerateFocusedAsync(
            Guid patientId,
            string focus,
            string title,
            IReadOnlyList<MedicalCvEvidenceItem> evidence,
            string language,
            MedicalCvCreatedByRole createdByRole,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakePreviewLinkService(DateTimeOffset expiresAt)
        : IMedicalCvPreviewLinkService
    {
        public Guid PatientId { get; private set; }
        public Guid MedicalCvId { get; private set; }
        public Guid VersionId { get; private set; }

        public MedicalCvPreviewLink Create(
            Guid patientId,
            Guid medicalCvId,
            Guid medicalCvVersionId)
        {
            PatientId = patientId;
            MedicalCvId = medicalCvId;
            VersionId = medicalCvVersionId;
            return new MedicalCvPreviewLink("token+/=", expiresAt);
        }

        public bool TryValidate(
            string token,
            Guid medicalCvVersionId,
            out MedicalCvPreviewAccess access) =>
            throw new NotSupportedException();
    }

    private sealed class FakeFileUrlResolver : IFileUrlResolver
    {
        public string ResolveFileUrl(string value) => ToAbsoluteUrl(value);

        public string ToAbsoluteUrl(string relativePath) =>
            $"https://api.test/{relativePath.TrimStart('/')}";
    }

    private sealed class PassThroughLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(
            bool includeParentCultures) => [];
    }
}
