using System.Globalization;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hakeem.Application.Features.PatientAccessSessions.Commands.CreatePatientAccessSession;

public sealed class CreatePatientAccessSessionCommandHandler(
    IPatientAccessRequestRepository accessRequestRepository,
    IDoctorProfileRepository doctorProfileRepository,
    IOneTimeAccessCodeService accessCodeService,
    ICurrentUserContext currentUserContext,
    IUnitOfWork unitOfWork,
    IOptions<PatientAccessConfiguration> options)
    : IRequestHandler<CreatePatientAccessSessionCommand, CreatePatientAccessSessionResult>
{
    private readonly TimeSpan _accessLifetime = TimeSpan.FromMinutes(
        options.Value.AccessLifetimeMinutes);

    public async Task<CreatePatientAccessSessionResult> Handle(
        CreatePatientAccessSessionCommand request,
        CancellationToken cancellationToken)
    {
        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        if (doctor.User.Status != AccountStatus.Active)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthAccountInactive);
        }

        var patientCode = request.PatientCode.Trim().ToUpperInvariant();
        var patient = await accessRequestRepository.GetPatientByCodeAsync(
            patientCode,
            cancellationToken);

        if (patient is null)
        {
            throw new UnprocessableEntityException(ErrorCodes.PatientAccessInvalidCode);
        }

        var candidates = await accessRequestRepository.GetCodeCandidatesAsync(
            doctor.Id,
            patient.Id,
            cancellationToken);

        var matchingRequest = FindMatchingRequest(
            candidates,
            PatientAccessRequestStatus.Approved,
            request.OneTimeCode) ??
            FindMatchingRequest(
                candidates,
                PatientAccessRequestStatus.Redeemed,
                request.OneTimeCode);

        if (matchingRequest is null)
        {
            throw new UnprocessableEntityException(ErrorCodes.PatientAccessInvalidCode);
        }

        if (matchingRequest.Status == PatientAccessRequestStatus.Redeemed)
        {
            throw new ConflictException(ErrorCodes.PatientAccessCodeAlreadyRedeemed);
        }

        var grantedAt = DateTime.UtcNow;
        if (!matchingRequest.CodeExpiresAt.HasValue ||
            matchingRequest.CodeExpiresAt.Value <= grantedAt)
        {
            await accessRequestRepository.ExpireApprovedAsync(
                matchingRequest.Id,
                doctor.Id,
                patient.Id,
                grantedAt,
                cancellationToken);
            throw new UnprocessableEntityException(ErrorCodes.PatientAccessCodeExpired);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await accessRequestRepository.ExpireStaleActiveAccessAsync(
                doctor.Id,
                patient.Id,
                grantedAt,
                cancellationToken);

            if (await accessRequestRepository.HasActiveAccessAsync(
                    doctor.Id,
                    patient.Id,
                    grantedAt,
                    cancellationToken))
            {
                throw new ConflictException(ErrorCodes.PatientAccessActiveAccessExists);
            }

            var affectedRows = await accessRequestRepository.RedeemApprovedAsync(
                matchingRequest.Id,
                doctor.Id,
                patient.Id,
                grantedAt,
                cancellationToken);

            if (affectedRows != 1)
            {
                await unitOfWork.RollBackTransactionAsync();
                var expiredRows = await accessRequestRepository.ExpireApprovedAsync(
                    matchingRequest.Id,
                    doctor.Id,
                    patient.Id,
                    DateTime.UtcNow,
                    cancellationToken);
                if (expiredRows == 1)
                {
                    throw new UnprocessableEntityException(
                        ErrorCodes.PatientAccessCodeExpired);
                }

                throw new ConflictException(ErrorCodes.PatientAccessCodeAlreadyRedeemed);
            }

            var access = new DoctorPatientAccess
            {
                Id = Guid.NewGuid(),
                PatientAccessRequestId = matchingRequest.Id,
                DoctorProfileId = doctor.Id,
                PatientProfileId = patient.Id,
                Status = DoctorPatientAccessStatus.Active,
                GrantedAt = grantedAt,
                ExpiresAt = grantedAt.Add(_accessLifetime)
            };

            accessRequestRepository.AddAccess(access);
            await unitOfWork.SaveChanges(cancellationToken);
            await unitOfWork.CommitTransactionAsync();

            return new CreatePatientAccessSessionResult(
                access.Id,
                patient.Id,
                new PatientAccessSessionPatient(
                    patient.PatientCode,
                    patient.FullName,
                    patient.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                access.GrantedAt,
                access.ExpiresAt);
        }
        catch (DbUpdateException exception)
            when (IsAccessUniquenessViolation(exception))
        {
            await unitOfWork.RollBackTransactionAsync();
            throw new ConflictException(ErrorCodes.PatientAccessActiveAccessExists);
        }
        catch
        {
            await unitOfWork.RollBackTransactionAsync();
            throw;
        }
    }

    private static bool IsAccessUniquenessViolation(DbUpdateException exception)
    {
        var details = exception.ToString();
        return details.Contains(
                "IX_DoctorPatientAccesses_DoctorProfileId_PatientProfileId",
                StringComparison.OrdinalIgnoreCase) ||
            details.Contains(
                "IX_DoctorPatientAccesses_PatientAccessRequestId",
                StringComparison.OrdinalIgnoreCase);
    }

    private PatientAccessRequest? FindMatchingRequest(
        IEnumerable<PatientAccessRequest> candidates,
        PatientAccessRequestStatus status,
        string code)
    {
        return candidates.FirstOrDefault(candidate =>
            candidate.Status == status &&
            candidate.CodeHash is not null &&
            accessCodeService.Verify(code, candidate.CodeHash));
    }
}
