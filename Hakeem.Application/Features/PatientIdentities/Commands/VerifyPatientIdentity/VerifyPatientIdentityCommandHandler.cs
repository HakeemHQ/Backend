using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Auth.Commands.Registration;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.PatientIdentities;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Application.Features.PatientIdentities.Commands.VerifyPatientIdentity;

public sealed class VerifyPatientIdentityCommandHandler(
    IPatientIdentityRepository patientIdentityRepository,
    IDoctorProfileRepository doctorProfileRepository,
    ICurrentUserContext currentUserContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VerifyPatientIdentityCommand, VerifyPatientIdentityResult>
{
    public async Task<VerifyPatientIdentityResult> Handle(
        VerifyPatientIdentityCommand request,
        CancellationToken cancellationToken)
    {
        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var patientCode = request.PatientCode.Trim().ToUpperInvariant();
        var nationalId = request.NationalId.Trim();

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var patient = await patientIdentityRepository.GetByPatientCodeForUpdateAsync(
                patientCode,
                cancellationToken);

            if (patient is null)
            {
                throw new NotFoundException(ErrorCodes.PatientIdentityNotFound);
            }

            if (!EgyptianNationalId.IsStructurallyValid(
                    nationalId,
                    DateOnly.FromDateTime(patient.BirthDate)))
            {
                throw new UnprocessableEntityException(
                    ErrorCodes.PatientIdentityNationalIdMismatch);
            }

            if (patient.IdentityVerificationStatus == IdentityVerificationStatus.Verified)
            {
                if (!string.Equals(
                        patient.VerifiedNationalId,
                        nationalId,
                        StringComparison.Ordinal))
                {
                    throw new ConflictException(
                        ErrorCodes.PatientIdentityAlreadyVerifiedWithDifferentNationalId);
                }

                await unitOfWork.CommitTransactionAsync();
                return CreateResult(patient, claimCorrected: false);
            }

            var belongsToAnotherPatient =
                await patientIdentityRepository.VerifiedNationalIdBelongsToAnotherPatientAsync(
                    nationalId,
                    patient.Id,
                    cancellationToken);

            if (belongsToAnotherPatient)
            {
                throw new ConflictException(
                    ErrorCodes.PatientIdentityNationalIdAlreadyVerified);
            }

            var claimCorrected = !string.Equals(
                patient.NationalId,
                nationalId,
                StringComparison.Ordinal);
            var verifiedAt = DateTime.UtcNow;

            patient.NationalId = nationalId;
            patient.VerifiedNationalId = nationalId;
            patient.VerifiedByDoctorId = doctor.Id;
            patient.VerifiedAt = verifiedAt;
            patient.IdentityVerificationStatus = IdentityVerificationStatus.Verified;

            await unitOfWork.SaveChanges(cancellationToken);
            await unitOfWork.CommitTransactionAsync();

            return CreateResult(patient, claimCorrected);
        }
        catch (DbUpdateException exception)
            when (IsVerifiedNationalIdUniquenessViolation(exception))
        {
            await unitOfWork.RollBackTransactionAsync();
            throw new ConflictException(
                ErrorCodes.PatientIdentityNationalIdAlreadyVerified);
        }
        catch
        {
            await unitOfWork.RollBackTransactionAsync();
            throw;
        }
    }

    private static VerifyPatientIdentityResult CreateResult(
        Hakeem.Domain.Entities.PatientProfile patient,
        bool claimCorrected)
    {
        if (!patient.VerifiedAt.HasValue)
        {
            throw new InvalidOperationException(
                "A verified patient identity must have a verification timestamp.");
        }

        return new VerifyPatientIdentityResult(
            patient.Id,
            patient.PatientCode,
            patient.FullName,
            patient.IdentityVerificationStatus.ToString(),
            claimCorrected,
            patient.VerifiedAt.Value);
    }

    private static bool IsVerifiedNationalIdUniquenessViolation(
        DbUpdateException exception)
    {
        return exception.ToString().Contains(
            "IX_PatientProfiles_VerifiedNationalId",
            StringComparison.OrdinalIgnoreCase);
    }
}
