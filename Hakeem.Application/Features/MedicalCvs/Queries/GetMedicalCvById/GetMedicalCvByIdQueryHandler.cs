using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Access;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvById;

public sealed class GetMedicalCvByIdQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IDoctorProfileRepository doctorProfileRepository,
    IDoctorPatientAccessRepository doctorPatientAccessRepository,
    IMedicalCvReadRepository medicalCvReadRepository)
    : IRequestHandler<GetMedicalCvByIdQuery, GetMedicalCvByIdResponse>
{
    public async Task<GetMedicalCvByIdResponse> Handle(
        GetMedicalCvByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.MedicalCvId == Guid.Empty)
        {
            throw new ArgumentException("Medical CV ID is required.");
        }

        // 1. Find the CV and get its patient
        var medicalCv = await medicalCvReadRepository.GetByIdAsync(
            request.MedicalCvId,
            cancellationToken);

        if (medicalCv is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvNotFound);
        }

        var patientId = medicalCv.PatientId;

        // 2. Check if current user is the patient
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is not null && patient.Id == patientId)
        {
            var medicalCvDetail =
                await medicalCvReadRepository.GetDetailForPatientAsync(
                    request.MedicalCvId,
                    patientId,
                    cancellationToken);

            if (medicalCvDetail is null)
            {
                throw new NotFoundException(ErrorCodes.MedicalCvNotFound);
            }

            return MapResponse(medicalCvDetail);
        }

        // 3. Current user is not the patient ? check doctor
        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        // 4. Doctor must have active access to this patient
        var accesses = await doctorPatientAccessRepository.GetForDoctorAsync(
            doctor.Id,
            DoctorPatientAccessStatus.Active,
            DateTime.UtcNow,
            1,
            100,
            cancellationToken);

        var hasAccess = accesses.Items.Any(x =>
            x.PatientProfileId == patientId);

        if (!hasAccess)
        {
            throw new ForbiddenException(
                "Doctor does not have active access to this patient.");
        }

        // 5. Authorized doctor ? get CV details
        var doctorMedicalCvDetail =
            await medicalCvReadRepository.GetDetailForPatientAsync(
                request.MedicalCvId,
                patientId,
                cancellationToken);

        if (doctorMedicalCvDetail is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvNotFound);
        }

        return MapResponse(doctorMedicalCvDetail);
    }
    private static GetMedicalCvByIdResponse MapResponse(MedicalCvDetailReadModel medicalCv)
    { var versions = medicalCv.Versions.Select(
        version => new MedicalCvVersionDetailResponse(
            version.MedicalCvVersionId,
            version.VersionNumber,
            version.Status, version.CreatedAt,
            version.ApprovedAt,
            version.PdfAvailable)).ToList();
        
        return new GetMedicalCvByIdResponse(medicalCv.MedicalCvId, 
            medicalCv.Title,
            medicalCv.ScopeType,
            medicalCv.Focus,
            medicalCv.CreatedAt,
            medicalCv.UpdatedAt,
            versions); }
}

