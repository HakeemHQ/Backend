using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.AuditLogs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using MediatR;
using System.Globalization;

namespace Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;

public sealed class GenerateFullMedicalCvCommandHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalCvGenerationService generationService,
    IMedicalCvPreviewLinkService previewLinkService,
    IAuditLogRepository auditLogRepository,
    IFileUrlResolver fileUrlResolver)
    : IRequestHandler<
        GenerateFullMedicalCvCommand,
        GenerateFullMedicalCvResponse>
{
    public async Task<GenerateFullMedicalCvResponse> Handle(
        GenerateFullMedicalCvCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var result = await generationService.GenerateFullAsync(
            patient.Id,
            request.Title,
            MedicalCvLanguages.Normalize(
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName),
            cancellationToken);

        auditLogRepository.Add(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = currentUserContext.UserId,
                PatientProfileId = patient.Id,
                Action = "MedicalCvGenerated",
                Target = $"MedicalCv:{result.MedicalCvId}",
                OccurredAt = DateTime.UtcNow
            });

        var previewLink = previewLinkService.Create(
            patient.Id,
            result.MedicalCvId,
            result.MedicalCvVersionId);
        var previewPath =
            $"medical-cv-versions/{result.MedicalCvVersionId}/preview?token=" +
            Uri.EscapeDataString(previewLink.Token);
        var pdfUrl = fileUrlResolver.ToAbsoluteUrl(previewPath);

        return new GenerateFullMedicalCvResponse(
            result.MedicalCvId,
            result.Title,
            result.ScopeType,
            new LatestMedicalCvVersionResponse(
                result.MedicalCvVersionId,
                result.VersionNumber,
                result.Status.ToString(),
                result.CreatedAt,
                pdfUrl,
                previewLink.ExpiresAt));
    }
}
