using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.PatientProfiles;
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
    IUnitOfWork unitOfWork,
    ILogger<MedicalCvGenerationService> logger)
    : IMedicalCvGenerationService, IScoped
{
    public Task<MedicalCvGenerationResult> GenerateFullAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync(
            patientId,
            MedicalCvScopeType.Full,
            focus: null,
            cancellationToken);
    }

    public Task<MedicalCvGenerationResult> GenerateFocusedAsync(
        Guid patientId,
        string focus,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focus);

        return GenerateAsync(
            patientId,
            MedicalCvScopeType.Focused,
            focus.Trim(),
            cancellationToken);
    }

    private async Task<MedicalCvGenerationResult> GenerateAsync(
        Guid patientId,
        MedicalCvScopeType scopeType,
        string? focus,
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

        IReadOnlyList<MedicalRecord> summarizedRecords = [];
        IReadOnlyList<MedicalCvEvidenceItem> evidence;

        if (scopeType == MedicalCvScopeType.Full)
        {
            summarizedRecords = await medicalRecordsRepository
                .GetAllConfirmedAsync(patientId, cancellationToken);

            evidence = summarizedRecords
                .Select(record => new MedicalCvEvidenceItem(
                    record.Id,
                    Score: null,
                    record.DisplayName,
                    record.RecordType,
                    record.ClinicalDate.ToString("yyyy-MM-dd")))
                .ToArray();
        }
        else
        {
            var searchResponse = await focusedMedicalEvidenceProvider.SearchAsync(
                patientId,
                focus!,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(searchResponse.GlobalErrorCode))
            {
                throw new InvalidOperationException(
                    $"Focused medical evidence retrieval failed with code '{searchResponse.GlobalErrorCode}'.");
            }

            evidence = searchResponse.Data
                .Select(item => new MedicalCvEvidenceItem(
                    item.PointId,
                    item.Score,
                    item.Content,
                    item.FieldName,
                    item.Value))
                .ToArray();
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
                scopeType,
                focus,
                evidence),
            cancellationToken);

        var medicalCv = await medicalCvRepository.GetByLogicalIdentityAsync(
            patientId,
            scopeType,
            focus,
            cancellationToken);

        if (medicalCv is null)
        {
            medicalCv = new MedicalCv
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Title = scopeType == MedicalCvScopeType.Full
                    ? $"Medical CV - {patient.FullName}"
                    : $"{focus} Medical CV - {patient.FullName}",
                ScopeType = scopeType,
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
                scopeType,
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

            foreach (var record in summarizedRecords)
            {
                version.SummarizedRecords.Add(record);
            }

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
            scopeType,
            medicalCv.Id,
            versionNumber,
            patientId);

        return new MedicalCvGenerationResult(
            medicalCv.Id,
            versionId,
            versionNumber,
            scopeType,
            focus,
            fileKey,
            version.Status);
    }
}
