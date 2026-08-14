using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.AuditLogs;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Commands.ApproveMedicalCvVersion;

public sealed class ApproveMedicalCvVersionCommandHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IDoctorProfileRepository doctorProfileRepository,
    IDoctorPatientAccessRepository doctorPatientAccessRepository,
    IMedicalCvRepository medicalCvRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<
        ApproveMedicalCvVersionCommand,
        ApproveMedicalCvVersionResponse>
{
    public async Task<ApproveMedicalCvVersionResponse> Handle(
        ApproveMedicalCvVersionCommand request,
        CancellationToken cancellationToken)
    {
        // First get the version and its patient.
        var version = await medicalCvRepository.GetVersionForApprovalAsync(
            request.MedicalCvVersionId,
            cancellationToken);

        if (version is null)
        {
            throw new NotFoundException(
                ErrorCodes.MedicalCvNotFound);
        }

        var patientId = version.MedicalCv.PatientId;

        // 1. Patient can approve his own CV.
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is not null && patient.Id == patientId)
        {
            return await ApproveVersionAsync(
                version,
                patientId,
                cancellationToken);
        }

        // 2. Otherwise, current user must be a Doctor.
        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            throw new UnAuthorizedException(
                ErrorCodes.AuthUnauthorized);
        }

        // 3. Doctor must have active access to this patient.
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

        // 4. Doctor is authorized.
        return await ApproveVersionAsync(
            version,
            patientId,
            cancellationToken);
    }

    private async Task<ApproveMedicalCvVersionResponse> ApproveVersionAsync(
        MedicalCvVersion version, Guid patientId,
        CancellationToken cancellationToken)
    {
        // Version must be in Draft state.
        if (version.Status != MedicalCvVersionStatus.Draft)
        {
            throw new ConflictException(
                ErrorCodes.MedicalCvVersionNotDraft);
        }

        var approvedAt = DateTime.UtcNow;

        // Only metadata/state changes.
        // The generated CV content remains unchanged.
        version.Status = MedicalCvVersionStatus.Approved;
        version.ApprovedAt = approvedAt;

        auditLogRepository.Add(
    new AuditLog
    {
        Id = Guid.NewGuid(),
        ActorUserId = currentUserContext.UserId,
        PatientProfileId = patientId,
        Action = "MedicalCvVersionApproved",
        Target = $"MedicalCvVersion:{version.Id}",
        OccurredAt = DateTime.UtcNow
    });
        await unitOfWork.SaveChanges(cancellationToken);

        return new ApproveMedicalCvVersionResponse(
            version.Id,
            version.VersionNumber,
            version.Status,
            approvedAt);
    }
}
