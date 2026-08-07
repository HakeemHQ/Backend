using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Services.MedicalCvs;

public sealed class MedicalCvGenerationProcessor(
    IMedicalCvRepository medicalCvRepository,
    IMedicalCvContentGenerator contentGenerator,
    IMedicalCvPdfGenerator pdfGenerator,
    IMedicalCvFileStorage fileStorage,
    IUnitOfWork unitOfWork,
    ILogger<MedicalCvGenerationProcessor> logger)
    : IMedicalCvGenerationProcessor, IScoped
{
    public async Task ProcessAsync(
        Guid medicalCvVersionId,
        CancellationToken cancellationToken)
    {
        if (medicalCvVersionId == Guid.Empty)
        {
            throw new ArgumentException(
                "A medical CV version ID is required.",
                nameof(medicalCvVersionId));
        }

        var version = await medicalCvRepository.GetVersionForGenerationAsync(
            medicalCvVersionId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Medical CV version '{medicalCvVersionId}' was not found.");

        if (version.Status is MedicalCvVersionStatus.Draft or MedicalCvVersionStatus.Approved &&
            !string.IsNullOrWhiteSpace(version.PdfFileKey))
        {
            logger.LogInformation(
                "Skipping completed medical CV version {MedicalCvVersionId}.",
                medicalCvVersionId);
            return;
        }

        if (version.MedicalCv.ScopeType != MedicalCvScopeType.Full)
        {
            throw new InvalidOperationException(
                "Only full medical CV generation is supported by this outbox request.");
        }

        version.Status = MedicalCvVersionStatus.Processing;
        await unitOfWork.SaveChanges(cancellationToken);

        string? savedFileKey = null;

        try
        {
            var evidence = version.SummarizedRecords
                .Where(record => !string.IsNullOrWhiteSpace(record.DisplayName))
                .Select(record => new MedicalCvEvidenceItem(
                    record.Id,
                    Score: null,
                    record.DisplayName,
                    record.RecordType,
                    record.ClinicalDate.ToString("yyyy-MM-dd")))
                .ToArray();

            if (evidence.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Medical CV version '{medicalCvVersionId}' has no confirmed evidence.");
            }

            var patient = version.MedicalCv.PatientProfile;
            var patientInformation = new MedicalCvPatientInformation(
                patient.FullName,
                patient.BirthDate,
                patient.User.Gender,
                patient.User.Email,
                patient.User.PhoneNumber);

            var content = await contentGenerator.GenerateAsync(
                new MedicalCvContentRequest(
                    patientInformation,
                    MedicalCvScopeType.Full,
                    Focus: null,
                    evidence,
                    version.MedicalCv.Title),
                cancellationToken);

            var pdfBytes = pdfGenerator.Generate(
                new MedicalCvPdfDocument(
                    patientInformation,
                    MedicalCvScopeType.Full,
                    Focus: null,
                    DateTime.UtcNow,
                    content));

            savedFileKey = await fileStorage.SaveAsync(
                pdfBytes,
                version.MedicalCvId,
                version.VersionNumber,
                cancellationToken);

            version.PdfFileKey = savedFileKey;
            version.Status = MedicalCvVersionStatus.Draft;
            await unitOfWork.SaveChanges(cancellationToken);

            logger.LogInformation(
                "Generated full medical CV {MedicalCvId}, version {VersionNumber}.",
                version.MedicalCvId,
                version.VersionNumber);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(savedFileKey))
            {
                try
                {
                    await fileStorage.DeleteAsync(
                        savedFileKey,
                        CancellationToken.None);
                }
                catch (Exception cleanupException)
                {
                    logger.LogError(
                        cleanupException,
                        "Failed to delete incomplete medical CV file {FileKey}.",
                        savedFileKey);
                }
            }

            version.PdfFileKey = string.Empty;
            version.Status = MedicalCvVersionStatus.Failed;

            try
            {
                await unitOfWork.SaveChanges(CancellationToken.None);
            }
            catch (Exception statusException)
            {
                logger.LogError(
                    statusException,
                    "Failed to persist failure status for medical CV version {MedicalCvVersionId}.",
                    medicalCvVersionId);
            }

            throw;
        }
    }
}
