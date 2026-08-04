using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecordById
{
    public sealed class GetMedicalRecordByIdQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalRecordsRepository medicalRecordRepository)
    : IRequestHandler<GetMedicalRecordByIdQuery, GetMedicalRecordByIdResult>
    {
        public async Task<GetMedicalRecordByIdResult> Handle(
            GetMedicalRecordByIdQuery request,
            CancellationToken cancellationToken)
        {
            var patient = await patientProfileRepository.GetByUserIdAsync(
                currentUserContext.UserId,
                cancellationToken);

            if (patient is null)
                throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);

            var medicalRecord =
                await medicalRecordRepository.GetMedicalRecordByIdAsync(
                    request.MedicalRecordId,
                    patient.Id,
                    cancellationToken);

            if (medicalRecord is null)
                throw new NotFoundException(ErrorCodes.MedicalRecordNotFound);

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
                    .ToList());
        }
    }
}
