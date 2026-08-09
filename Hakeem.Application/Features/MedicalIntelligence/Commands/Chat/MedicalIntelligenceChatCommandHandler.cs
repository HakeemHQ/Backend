using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.MedicalIntelligence.Commands.Chat;

public sealed class MedicalIntelligenceChatCommandHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalIntelligenceAgent medicalIntelligenceAgent,
    IMedicalCvPreviewLinkService previewLinkService,
    IFileUrlResolver fileUrlResolver)
    : IRequestHandler<
        MedicalIntelligenceChatCommand,
        MedicalIntelligenceChatResponse>
{
    public async Task<MedicalIntelligenceChatResponse> Handle(
        MedicalIntelligenceChatCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var agentResponse = await medicalIntelligenceAgent.RespondAsync(
            patient.Id,
            request.Message.Trim(),
            cancellationToken);

        var ragResults = agentResponse.RagResults
            .Select(result => new MedicalIntelligenceRagResultResponse(
                result.MedicalRecordId,
                result.Score,
                result.RecordType,
                result.DisplayName,
                result.Status,
                result.ClinicalDate,
                result.Content,
                result.Fields))
            .ToArray();

        GeneratedFocusedMedicalCvResponse? generatedCv = null;

        if (agentResponse.FocusedMedicalCv is not null)
        {
            var cv = agentResponse.FocusedMedicalCv;
            var previewLink = previewLinkService.Create(
                patient.Id,
                cv.MedicalCvId,
                cv.MedicalCvVersionId);
            var previewPath =
                $"medical-cv-versions/{cv.MedicalCvVersionId}/preview?token=" +
                Uri.EscapeDataString(previewLink.Token);

            generatedCv = new GeneratedFocusedMedicalCvResponse(
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

        return new MedicalIntelligenceChatResponse(
            agentResponse.Message,
            ragResults,
            generatedCv);
    }
}
