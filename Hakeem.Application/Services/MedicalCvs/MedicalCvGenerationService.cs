using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Services.MedicalCvs;

public sealed class MedicalCvGenerationService(
    IPatientProfileRepository patientProfileRepository,
    IMedicalRecordsRepository medicalRecordsRepository,
    IMedicalCvRepository medicalCvRepository,
    IMedicalCvContentGenerator contentGenerator,
    IMedicalCvPdfGenerator pdfGenerator,
    IMedicalCvFileStorage fileStorage,
    IOutboxEventRepository outboxEventRepository,
    IUnitOfWork unitOfWork,
    ILogger<MedicalCvGenerationService> logger)
    : IMedicalCvGenerationService, IScoped
{
    public async Task<MedicalCvGenerationResult> GenerateFullAsync(
        Guid patientId,
        string title,
        string language,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException(
                "A patient ID is required.",
                nameof(patientId));
        }

        var patient = await patientProfileRepository.GetByIdAsync(
            patientId,
            cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvPatientNotFound);
        }

        var summarizedRecords = (await medicalRecordsRepository
                .GetAllConfirmedAsync(patientId, cancellationToken))
            .Where(record => !string.IsNullOrWhiteSpace(record.DisplayName))
            .ToArray();

        if (summarizedRecords.Length == 0)
        {
            throw new UnprocessableEntityException(
                ErrorCodes.MedicalCvNoConfirmedInformation);
        }

        var medicalCv = await medicalCvRepository.GetByLogicalIdentityAsync(
            patientId,
            MedicalCvScopeType.Full,
            focus: null,
            cancellationToken);

        if (medicalCv is null)
        {
            medicalCv = new MedicalCv
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Title = title.Trim(),
                ScopeType = MedicalCvScopeType.Full,
                Focus = null
            };

            medicalCvRepository.Add(medicalCv);
        }

        var version = new MedicalCvVersion
        {
            Id = Guid.NewGuid(),
            MedicalCvId = medicalCv.Id,
            VersionNumber = await medicalCvRepository.GetNextVersionNumberAsync(
                medicalCv.Id,
                cancellationToken),
            Status = MedicalCvVersionStatus.Queued,
            PdfFileKey = string.Empty
        };

        foreach (var record in summarizedRecords)
        {
            version.SummarizedRecords.Add(record);
        }

        medicalCvRepository.AddVersion(version);
        outboxEventRepository.Add(
            new CreateMedicalCvRequest
            {
                MedicalCvVersionId = version.Id,
                Language = MedicalCvLanguages.Normalize(language)
            },
            $"medical-cv-generation:{version.Id}");

        await unitOfWork.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Queued full medical CV {MedicalCvId}, version {VersionNumber}, for patient {PatientId}.",
            medicalCv.Id,
            version.VersionNumber,
            patientId);

        return new MedicalCvGenerationResult(
            medicalCv.Id,
            version.Id,
            medicalCv.Title,
            version.VersionNumber,
            MedicalCvScopeType.Full,
            Focus: null,
            version.PdfFileKey,
            version.Status,
            version.CreatedAt);
    }

    public Task<MedicalCvGenerationResult> GenerateFocusedAsync(
        Guid patientId,
        string focus,
        string title,
        IReadOnlyList<MedicalCvEvidenceItem> evidence,
        string language,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focus);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(evidence);

        if (focus.Trim().Length > 200)
        {
            throw new ArgumentException(
                "The focused medical CV focus cannot exceed 200 characters.",
                nameof(focus));
        }

        if (title.Trim().Length > 200)
        {
            throw new ArgumentException(
                "The focused medical CV title cannot exceed 200 characters.",
                nameof(title));
        }

        if (evidence.Count == 0)
        {
            throw new UnprocessableEntityException(
                ErrorCodes.MedicalCvNoConfirmedInformation);
        }

        return GenerateFocusedInternalAsync(
            patientId,
            focus.Trim(),
            title.Trim(),
            evidence,
            MedicalCvLanguages.Normalize(language),
            cancellationToken);
    }

    private async Task<MedicalCvGenerationResult> GenerateFocusedInternalAsync(
        Guid patientId,
        string focus,
        string title,
        IReadOnlyList<MedicalCvEvidenceItem> evidence,
        string language,
        CancellationToken cancellationToken)
    {
        if (patientId == Guid.Empty)
        {
            throw new ArgumentException(
                "A patient ID is required.",
                nameof(patientId));
        }

        var patient = await patientProfileRepository.GetByIdAsync(
            patientId,
            cancellationToken);

        if (patient is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvPatientNotFound);
        }

        var medicalCv = await medicalCvRepository.GetByLogicalIdentityAsync(
            patientId,
            MedicalCvScopeType.Focused,
            focus,
            cancellationToken);

        if (medicalCv is not null)
        {
            medicalCv.Title = title;
        }

        var patientInformation = new MedicalCvPatientInformation(
            patient.FullName,
            patient.BirthDate,
            patient.User.Gender,
            patient.User.Email,
            patient.User.PhoneNumber);

        var content = await contentGenerator.GenerateAsync(
            new MedicalCvContentRequest(
                patientInformation,
                MedicalCvScopeType.Focused,
                focus,
                evidence,
                title,
                language),
            cancellationToken);

        if (medicalCv is null)
        {
            medicalCv = new MedicalCv
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Title = title,
                ScopeType = MedicalCvScopeType.Focused,
                Focus = focus
            };

            medicalCvRepository.Add(medicalCv);
        }

        var versionNumber = await medicalCvRepository.GetNextVersionNumberAsync(
            medicalCv.Id,
            cancellationToken);
        var versionId = Guid.NewGuid();
        var generatedAtUtc = DateTime.UtcNow;
        var pdfBytes = pdfGenerator.Generate(
            new MedicalCvPdfDocument(
                patientInformation,
                MedicalCvScopeType.Focused,
                focus,
                generatedAtUtc,
                content,
                language));
        var fileKey = await fileStorage.SaveAsync(
            pdfBytes,
            medicalCv.Id,
            versionNumber,
            cancellationToken);

        MedicalCvVersion version;
        try
        {
            version = new MedicalCvVersion
            {
                Id = versionId,
                MedicalCvId = medicalCv.Id,
                VersionNumber = versionNumber,
                Status = MedicalCvVersionStatus.Draft,
                PdfFileKey = fileKey
            };

            medicalCvRepository.AddVersion(version);
            await unitOfWork.SaveChanges(cancellationToken);
        }
        catch
        {
            try
            {
                await fileStorage.DeleteAsync(fileKey, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(
                    cleanupException,
                    "Failed to delete orphaned medical CV file {FileKey}.",
                    fileKey);
            }

            throw;
        }

        logger.LogInformation(
            "Generated {ScopeType} medical CV {MedicalCvId}, version {VersionNumber}, for patient {PatientId}.",
            MedicalCvScopeType.Focused,
            medicalCv.Id,
            versionNumber,
            patientId);

        return new MedicalCvGenerationResult(
            medicalCv.Id,
            versionId,
            medicalCv.Title,
            versionNumber,
            MedicalCvScopeType.Focused,
            focus,
            fileKey,
            version.Status,
            version.CreatedAt);
    }
}
