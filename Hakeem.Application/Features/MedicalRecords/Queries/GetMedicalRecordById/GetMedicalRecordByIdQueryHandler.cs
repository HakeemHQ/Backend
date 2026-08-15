using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecordById;

public sealed class GetMedicalRecordByIdQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IDoctorProfileRepository doctorProfileRepository,
    IDoctorPatientAccessRepository doctorPatientAccessRepository,
    IMedicalRecordsRepository medicalRecordRepository)
    : IRequestHandler<GetMedicalRecordByIdQuery, GetMedicalRecordByIdResult>
{
    public async Task<GetMedicalRecordByIdResult> Handle(
        GetMedicalRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        var medicalRecord = await medicalRecordRepository.GetByIdWithDetailsAsync(
            request.MedicalRecordId,
            cancellationToken);

        if (medicalRecord is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalRecordNotFound);
        }

        if (!await CanAccessRecordAsync(medicalRecord.PatientProfileId, cancellationToken))
        {
            throw new NotFoundException(ErrorCodes.MedicalRecordNotFound);
        }

        return new GetMedicalRecordByIdResult(
            medicalRecord.Id,
            medicalRecord.RecordType,
            medicalRecord.DisplayName,
            medicalRecord.ClinicalDate,
            medicalRecord.Status,
            medicalRecord.SourceReferences.Select(source =>
                new SourceDto(
                    source.DocumentId,
                    source.MedicalDocument.Title,
                    source.PageReference))
                .ToList(),
            medicalRecord.Fields
                .Select(field => new MedicalRecordFieldDTO(
                    field.Id,
                    field.FieldName,
                    field.Value))
                .ToList());
    }

    private async Task<bool> CanAccessRecordAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is not null && patient.Id == patientProfileId)
        {
            return true;
        }

        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            return false;
        }

        return await doctorPatientAccessRepository.HasActiveAccessAsync(
            doctor.Id,
            patientProfileId,
            DateTime.UtcNow,
            cancellationToken);
    }
}
