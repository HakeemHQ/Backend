using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hakeem.Application.Features.PatientAccessRequests.Commands.CreatePatientAccessRequest;

public sealed class CreatePatientAccessRequestCommandHandler(
    IPatientAccessRequestRepository accessRequestRepository,
    IDoctorProfileRepository doctorProfileRepository,
    IOutboxEventRepository outboxEventRepository,
    ICurrentUserContext currentUserContext,
    IUnitOfWork unitOfWork,
    IOptions<PatientAccessConfiguration> options)
    : IRequestHandler<CreatePatientAccessRequestCommand, CreatePatientAccessRequestResult>
{
    private readonly TimeSpan _pendingRequestLifetime = TimeSpan.FromMinutes(
        options.Value.PendingRequestLifetimeMinutes);

    public async Task<CreatePatientAccessRequestResult> Handle(
        CreatePatientAccessRequestCommand request,
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
        var requestedAt = DateTime.UtcNow;

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var patient = await accessRequestRepository.GetPatientByCodeAsync(
                patientCode,
                cancellationToken);

            if (patient is null)
            {
                throw new NotFoundException(ErrorCodes.PatientAccessPatientNotFound);
            }

            if (patient.IdentityVerificationStatus != IdentityVerificationStatus.Verified)
            {
                throw new UnprocessableEntityException(
                    ErrorCodes.PatientAccessPatientNotVerified);
            }

            var pendingExpiresBefore = requestedAt.Subtract(_pendingRequestLifetime);
            await accessRequestRepository.ExpireStaleRequestsAsync(
                patient.Id,
                pendingExpiresBefore,
                requestedAt,
                cancellationToken);

            if (await accessRequestRepository.HasBlockingRequestAsync(
                    doctor.Id,
                    patient.Id,
                    pendingExpiresBefore,
                    requestedAt,
                    cancellationToken))
            {
                throw new ConflictException(
                    ErrorCodes.PatientAccessPendingRequestExists);
            }

            if (await accessRequestRepository.HasActiveAccessAsync(
                    doctor.Id,
                    patient.Id,
                    requestedAt,
                    cancellationToken))
            {
                throw new ConflictException(
                    ErrorCodes.PatientAccessActiveAccessExists);
            }

            var accessRequest = new PatientAccessRequest
            {
                Id = Guid.NewGuid(),
                DoctorProfileId = doctor.Id,
                PatientProfileId = patient.Id,
                Status = PatientAccessRequestStatus.Pending,
                RequestedAt = requestedAt
            };

            accessRequestRepository.Add(accessRequest);
            outboxEventRepository.Add(
                new PatientAccessRequestedEvent
                {
                    RequestId = accessRequest.Id,
                    PatientUserId = patient.UserId,
                    OccurredOn = requestedAt
                },
                $"patient-access-requested:{accessRequest.Id}");

            await unitOfWork.SaveChanges(cancellationToken);
            await unitOfWork.CommitTransactionAsync();

            return new CreatePatientAccessRequestResult(
                accessRequest.Id,
                accessRequest.Status.ToString(),
                accessRequest.RequestedAt);
        }
        catch (DbUpdateException exception)
            when (IsPendingRequestUniquenessViolation(exception))
        {
            await unitOfWork.RollBackTransactionAsync();
            throw new ConflictException(
                ErrorCodes.PatientAccessPendingRequestExists);
        }
        catch
        {
            await unitOfWork.RollBackTransactionAsync();
            throw;
        }
    }

    private static bool IsPendingRequestUniquenessViolation(
        DbUpdateException exception)
    {
        return exception.ToString().Contains(
            "IX_PatientAccessRequests_DoctorProfileId_PatientProfileId",
            StringComparison.OrdinalIgnoreCase);
    }
}
