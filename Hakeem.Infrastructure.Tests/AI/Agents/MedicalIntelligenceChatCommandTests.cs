using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalIntelligence.Commands.Chat;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class MedicalIntelligenceChatCommandTests
{
    [Fact]
    public async Task Handle_PatientEvidence_ReturnsRagListWithoutPreviewLink()
    {
        var patient = new PatientProfile { Id = Guid.NewGuid() };
        var recordId = Guid.NewGuid();
        var agent = new FakeMedicalIntelligenceAgent(
            new MedicalIntelligenceResponse(
                "Your records list Metformin 500 mg twice daily.",
                MedicalIntelligenceCapability.PatientEvidence,
                [
                    new PatientMedicalEvidenceItem(
                        recordId,
                        0.83f,
                        "Medication",
                        "MedicationName: Metformin",
                        "Confirmed",
                        new DateTime(2026, 8, 1),
                        "MedicationName: Metformin; Dose: 500 mg",
                        "Dose: 500 mg; Frequency: twice daily")
                ]));
        var previewService = new FakePreviewLinkService();
        var handler = CreateHandler(patient, agent, previewService);

        var result = await handler.Handle(
            new MedicalIntelligenceChatCommand(
                "  What diabetes medicine am I taking?  "),
            CancellationToken.None);

        Assert.Equal(patient.Id, agent.PatientId);
        Assert.Equal(
            "What diabetes medicine am I taking?",
            agent.Message);
        Assert.Equal(
            "Your records list Metformin 500 mg twice daily.",
            result.Message);
        var ragResult = Assert.Single(result.RagResults);
        Assert.Equal(recordId, ragResult.MedicalRecordId);
        Assert.Equal(0.83f, ragResult.Score);
        Assert.Equal("Medication", ragResult.RecordType);
        Assert.Null(result.GeneratedCv);
        Assert.Equal(0, previewService.CallCount);
    }

    [Fact]
    public async Task Handle_GeneratedCv_ReturnsSignedAbsolutePreviewUrl()
    {
        var patient = new PatientProfile { Id = Guid.NewGuid() };
        var medicalCvId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var createdAt = new DateTime(
            2026,
            8,
            8,
            10,
            0,
            0,
            DateTimeKind.Utc);
        var agent = new FakeMedicalIntelligenceAgent(
            new MedicalIntelligenceResponse(
                "Diabetes Medical CV version 1 was generated.",
                MedicalIntelligenceCapability.FocusedMedicalCv,
                [],
                new FocusedMedicalCvActionResult(
                    medicalCvId,
                    versionId,
                    "Diabetes Medical CV",
                    "Diabetes",
                    1,
                    MedicalCvVersionStatus.Draft,
                    createdAt)));
        var previewService = new FakePreviewLinkService();
        var handler = CreateHandler(patient, agent, previewService);

        var result = await handler.Handle(
            new MedicalIntelligenceChatCommand(
                "Create a medical CV for my diabetes."),
            CancellationToken.None);

        Assert.Empty(result.RagResults);
        Assert.NotNull(result.GeneratedCv);
        Assert.Equal(medicalCvId, result.GeneratedCv.MedicalCvId);
        Assert.Equal(versionId, result.GeneratedCv.MedicalCvVersionId);
        Assert.Equal("Draft", result.GeneratedCv.Status);
        Assert.Equal(
            $"https://api.test/medical-cv-versions/{versionId}/preview?token=token%2B%2F%3D",
            result.GeneratedCv.PreviewUrl);
        Assert.Equal(previewService.ExpiresAt, result.GeneratedCv.PreviewExpiresAt);
        Assert.Equal(patient.Id, previewService.PatientId);
        Assert.Equal(medicalCvId, previewService.MedicalCvId);
        Assert.Equal(versionId, previewService.VersionId);
    }

    [Fact]
    public async Task Handle_Refusal_ReturnsMessageWithoutEitherPayload()
    {
        var patient = new PatientProfile { Id = Guid.NewGuid() };
        var agent = new FakeMedicalIntelligenceAgent(
            new MedicalIntelligenceResponse(
                "I can only answer questions using your medical records.",
                MedicalIntelligenceCapability.None,
                []));
        var previewService = new FakePreviewLinkService();
        var handler = CreateHandler(patient, agent, previewService);

        var result = await handler.Handle(
            new MedicalIntelligenceChatCommand("Recommend a treatment."),
            CancellationToken.None);

        Assert.Empty(result.RagResults);
        Assert.Null(result.GeneratedCv);
        Assert.Contains("only answer", result.Message);
        Assert.Equal(0, previewService.CallCount);
    }

    [Fact]
    public async Task Handle_WithoutPatientProfile_ThrowsUnauthorized()
    {
        var agent = new FakeMedicalIntelligenceAgent(
            new MedicalIntelligenceResponse(
                "unused",
                MedicalIntelligenceCapability.None,
                []));
        var handler = CreateHandler(
            patient: null,
            agent,
            new FakePreviewLinkService());

        await Assert.ThrowsAsync<UnAuthorizedException>(
            () => handler.Handle(
                new MedicalIntelligenceChatCommand("My medication?"),
                CancellationToken.None));

        Assert.Equal(0, agent.CallCount);
    }

    [Fact]
    public void Validator_RejectsEmptyAndOversizedMessages()
    {
        var validator = new MedicalIntelligenceChatCommandValidator();

        Assert.False(validator.Validate(
            new MedicalIntelligenceChatCommand(" ")).IsValid);
        Assert.False(validator.Validate(
            new MedicalIntelligenceChatCommand(new string('x', 4_001))).IsValid);
        Assert.True(validator.Validate(
            new MedicalIntelligenceChatCommand("What medication am I taking?")).IsValid);
    }

    private static MedicalIntelligenceChatCommandHandler CreateHandler(
        PatientProfile? patient,
        IMedicalIntelligenceAgent agent,
        FakePreviewLinkService previewService) =>
        new(
            new FakeCurrentUserContext(),
            new FakePatientProfileRepository(patient),
            agent,
            previewService,
            new FakeFileUrlResolver());

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid UserId { get; } = Guid.NewGuid();
    }

    private sealed class FakePatientProfileRepository(PatientProfile? patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(patient);

        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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

    private sealed class FakeMedicalIntelligenceAgent(
        MedicalIntelligenceResponse response)
        : IMedicalIntelligenceAgent
    {
        public int CallCount { get; private set; }
        public Guid PatientId { get; private set; }
        public string? Message { get; private set; }

        public Task<MedicalIntelligenceResponse> RespondAsync(
            Guid patientId,
            string message,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            PatientId = patientId;
            Message = message;
            return Task.FromResult(response);
        }
    }

    private sealed class FakePreviewLinkService
        : IMedicalCvPreviewLinkService
    {
        public int CallCount { get; private set; }
        public Guid PatientId { get; private set; }
        public Guid MedicalCvId { get; private set; }
        public Guid VersionId { get; private set; }
        public DateTimeOffset ExpiresAt { get; } =
            new(2026, 8, 8, 10, 15, 0, TimeSpan.Zero);

        public MedicalCvPreviewLink Create(
            Guid patientId,
            Guid medicalCvId,
            Guid medicalCvVersionId)
        {
            CallCount++;
            PatientId = patientId;
            MedicalCvId = medicalCvId;
            VersionId = medicalCvVersionId;
            return new MedicalCvPreviewLink("token+/=", ExpiresAt);
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
}
