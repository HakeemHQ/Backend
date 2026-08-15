using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Services.Access;
using MediatR;

namespace Hakeem.Application.Features.Doctor.MedicalIntelligence.Commands.Chat;

public sealed class DoctorMedicalIntelligenceChatCommandHandler(
    IDoctorPatientAccessGuard doctorPatientAccessGuard,
    IMedicalIntelligenceAgent medicalIntelligenceAgent,
    IMedicalCvPreviewLinkService previewLinkService,
    IFileUrlResolver fileUrlResolver)
    : IRequestHandler<DoctorMedicalIntelligenceChatCommand, DoctorMedicalIntelligenceChatResponse>
{
    public async Task<DoctorMedicalIntelligenceChatResponse> Handle(
        DoctorMedicalIntelligenceChatCommand request,
        CancellationToken cancellationToken)
    {
        await doctorPatientAccessGuard.RequireDoctorWithActiveAccessAsync(
            request.PatientProfileId,
            cancellationToken);

        var agentResponse = await medicalIntelligenceAgent.RespondAsync(
            request.PatientProfileId,
            request.Message.Trim(),
            cancellationToken);

        var ragResults = agentResponse.RagResults
            .Select(result => new DoctorMedicalIntelligenceRagResultResponse(
                result.MedicalRecordId,
                result.Score,
                result.RecordType,
                result.DisplayName,
                result.Status,
                result.ClinicalDate,
                result.Content,
                result.Fields))
            .ToArray();

        DoctorGeneratedFocusedMedicalCvResponse? generatedCv = null;

        if (agentResponse.FocusedMedicalCv is not null)
        {
            var cv = agentResponse.FocusedMedicalCv;
            var previewLink = previewLinkService.Create(
                request.PatientProfileId,
                cv.MedicalCvId,
                cv.MedicalCvVersionId);
            var previewPath =
                $"medical-cv-versions/{cv.MedicalCvVersionId}/preview?token=" +
                Uri.EscapeDataString(previewLink.Token);

            generatedCv = new DoctorGeneratedFocusedMedicalCvResponse(
                cv.MedicalCvId,
                cv.MedicalCvVersionId,
                cv.Title,
                cv.Focus,
                cv.VersionNumber,
                cv.Status.ToString(),
                cv.CreatedAt,
                fileUrlResolver.ToAbsoluteUrl(previewPath),
                previewLink.ExpiresAt);
        }

        return new DoctorMedicalIntelligenceChatResponse(
            agentResponse.Message,
            ragResults,
            generatedCv);
    }
}
