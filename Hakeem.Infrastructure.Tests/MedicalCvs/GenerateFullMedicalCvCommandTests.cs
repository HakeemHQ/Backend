using System.Globalization;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Application.Resources;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Infrastructure.Tests.Fakes;
using Microsoft.Extensions.Localization;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class GenerateFullMedicalCvCommandTests
{
    [Fact]
    public async Task Handler_ReturnsQueuedVersionContractForPatient()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        var generatedAt = new DateTime(
            2026,
            7,
            19,
            13,
            20,
            0,
            DateTimeKind.Utc);
        var generationService = new FakeGenerationService(
            new MedicalCvGenerationResult(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Mazen Medical CV",
                1,
                MedicalCvScopeType.Full,
                null,
                "medical-cvs/cv/version-1.pdf",
                MedicalCvVersionStatus.Queued,
                generatedAt,
                userId,
                MedicalCvCreatedByRole.Patient));
        var handler = new GenerateFullMedicalCvCommandHandler(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            generationService);

        var response = await handler.Handle(
            new GenerateFullMedicalCvCommand("Mazen Medical CV"),
            CancellationToken.None);

        Assert.Equal(generationService.Result.MedicalCvId, response.MedicalCvId);
        Assert.Equal("Mazen Medical CV", response.Title);
        Assert.Equal(
            generationService.Result.MedicalCvVersionId,
            response.LatestVersion.MedicalCvVersionId);
        Assert.Equal(1, response.LatestVersion.VersionNumber);
        Assert.Equal("Queued", response.LatestVersion.GenerationStatus);
        Assert.Equal("Unreviewed", response.LatestVersion.VerificationStatus);
        Assert.Equal("Patient", response.LatestVersion.CreatedByRole);
        Assert.Equal(
            MedicalCvCreatedByRole.Patient,
            generationService.CreatedByRole);
        Assert.Equal("Mazen Medical CV", generationService.Title);
        Assert.Equal("en", generationService.Language);
    }

    [Fact]
    public async Task Handler_UsesArabicRequestCultureForGeneration()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var userId = Guid.NewGuid();
            var patient = new PatientProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            var generationService = new FakeGenerationService(
                new MedicalCvGenerationResult(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "السيرة الطبية",
                    1,
                    MedicalCvScopeType.Full,
                    null,
                    string.Empty,
                    MedicalCvVersionStatus.Queued,
                    DateTime.UtcNow,
                    userId,
                    MedicalCvCreatedByRole.Patient));
            var handler = new GenerateFullMedicalCvCommandHandler(
                new FakeCurrentUserContext(userId),
                new FakePatientProfileRepository(patient),
                generationService);

            await handler.Handle(
                new GenerateFullMedicalCvCommand("السيرة الطبية"),
                CancellationToken.None);

            Assert.Equal("ar", generationService.Language);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task Validator_RejectsMissingAndOversizedTitles()
    {
        var validator = new GenerateFullMedicalCvCommandValidator(
            new PassThroughLocalizer());

        var missingResult = await validator.ValidateAsync(
            new GenerateFullMedicalCvCommand(string.Empty));
        var oversizedResult = await validator.ValidateAsync(
            new GenerateFullMedicalCvCommand(new string('x', 201)));

        Assert.False(missingResult.IsValid);
        Assert.False(oversizedResult.IsValid);
    }

    private sealed record FakeCurrentUserContext(Guid UserId)
        : ICurrentUserContext;

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

    private sealed class FakeGenerationService(MedicalCvGenerationResult result)
        : IMedicalCvGenerationService
    {
        public MedicalCvGenerationResult Result { get; } = result;
        public string? Title { get; private set; }
        public string? Language { get; private set; }
        public MedicalCvCreatedByRole? CreatedByRole { get; private set; }

        public Task<MedicalCvGenerationResult> GenerateFullAsync(
            Guid patientId,
            string title,
            string language,
            MedicalCvCreatedByRole createdByRole,
            CancellationToken cancellationToken = default)
        {
            Title = title;
            Language = language;
            CreatedByRole = createdByRole;
            return Task.FromResult(Result);
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

    private sealed class PassThroughLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(CultureInfo.InvariantCulture, name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(
            bool includeParentCultures) => [];
    }
}
