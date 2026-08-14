using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.AuditLogs;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.MedicalCvs;
using MediatR;
using static Hakeem.Application.Repositories.MedicalCvs.IMedicalCvRepository;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;

public sealed class GetMedicalCvPdfQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalCvRepository medicalCvRepository,
    IDoctorProfileRepository doctorProfileRepository,
    IDoctorPatientAccessRepository doctorPatientAccessRepository,
    IMedicalCvReadRepository medicalCvReadRepository,
    IAuditLogRepository auditLogRepository,
    IMedicalCvFileStorage fileStorage)
    : IRequestHandler<GetMedicalCvPdfQuery, GetMedicalCvPdfResult>
{
    public async Task<GetMedicalCvPdfResult> Handle(
        GetMedicalCvPdfQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Get the medical CV version first.
        // This tells us which patient owns the CV.
        var version = await medicalCvRepository.GetVersionByIdAsync(
            request.MedicalCvVersionId,
            cancellationToken);

        if (version is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvNotFound);
        }
        var medicalCv = await medicalCvReadRepository.GetByIdAsync(
    version.MedicalCvId,
    cancellationToken);

        if (medicalCv is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvNotFound);
        }
        var patientId = medicalCv.PatientId;

        // 2. Check if the current user is the patient
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is not null && patient.Id == patientId)
        {
            return await GetPdfAsync(
                version,
                cancellationToken);
        }

        // 3. Current user is not the patient.
        // Check whether the current user is a doctor.
        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            throw new UnAuthorizedException(
                ErrorCodes.AuthUnauthorized);
        }

        // 4. Doctor must have active access to this patient.
        var accesses = await doctorPatientAccessRepository.GetForDoctorAsync(
            doctor.Id,
            DoctorPatientAccessStatus.Active,
            DateTime.UtcNow,
            1,
            100,
            cancellationToken);

        var hasAccess = accesses.Items.Any(
            x => x.PatientProfileId == patientId);

        if (!hasAccess)
        {
            throw new ForbiddenException(
                "Doctor does not have active access to this patient.");
        }

        // 5. Reuse the same PDF retrieval logic.
        return await GetPdfAsync(
            version,
            cancellationToken);
    }

    private async Task<GetMedicalCvPdfResult> GetPdfAsync(
        MedicalCvVersionReadModel version,
        CancellationToken cancellationToken)
    {
        if (version.Status is
            MedicalCvVersionStatus.Queued or
            MedicalCvVersionStatus.Processing)
        {
            throw new ConflictException(
                ErrorCodes.MedicalCvNotReady);
        }

        if (version.Status == MedicalCvVersionStatus.Failed)
        {
            throw new ServiceUnavailableException(
                ErrorCodes.MedicalCvGenerationFailed);
        }

        if (string.IsNullOrWhiteSpace(version.PdfFileKey))
        {
            throw new ConflictException(
                ErrorCodes.MedicalCvNotReady);
        }

        var content = await fileStorage.OpenReadAsync(
            version.PdfFileKey,
            cancellationToken);

        return new GetMedicalCvPdfResult(content);
    }

  
}
