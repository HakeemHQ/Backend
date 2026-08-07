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
    IFocusedMedicalEvidenceProvider focusedMedicalEvidenceProvider,
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
                MedicalCvVersionId = version.Id
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
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focus);

        return GenerateFocusedInternalAsync(
            patientId,
            focus.Trim(),
            cancellationToken);
    }

    private async Task<MedicalCvGenerationResult> GenerateFocusedInternalAsync(
        Guid patientId,
        string focus,
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

        var searchResponse = await focusedMedicalEvidenceProvider.SearchAsync(
            patientId,
            focus,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(searchResponse.GlobalErrorCode))
        {
            throw new InvalidOperationException(
                $"Focused medical evidence retrieval failed with code '{searchResponse.GlobalErrorCode}'.");
        }

        var evidence = searchResponse.Data
            .Select(item => new MedicalCvEvidenceItem(
                item.PointId,
                item.Score,
                item.Content,
                item.FieldName,
                item.Value))
            .ToArray();

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
                evidence),
            cancellationToken);

        var medicalCv = await medicalCvRepository.GetByLogicalIdentityAsync(
            patientId,
            MedicalCvScopeType.Focused,
            focus,
            cancellationToken);

        if (medicalCv is null)
        {
            medicalCv = new MedicalCv
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Title = $"{focus} Medical CV - {patient.FullName}",
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
                content));
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
