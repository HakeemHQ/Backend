using Hakeem.Application.Common;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.PatientAccessRequests;

public interface IPatientAccessRequestRepository : IScoped
{
    Task<PatientProfile?> GetPatientByCodeAsync(
        string patientCode,
        CancellationToken cancellationToken);

    Task<int> ExpireStaleRequestsAsync(
        Guid patientProfileId,
        DateTime pendingExpiresBefore,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<bool> HasBlockingRequestAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime pendingExpiresBefore,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<bool> HasActiveAccessAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    void Add(PatientAccessRequest accessRequest);

    Task<PaginatedResult<PatientAccessRequest>> GetForPatientAsync(
        Guid patientProfileId,
        Hakeem.Domain.Enums.Access.PatientAccessRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PatientAccessRequest?> GetByIdForPatientAsync(
        Guid requestId,
        Guid patientProfileId,
        CancellationToken cancellationToken);

    Task<int> ApprovePendingAsync(
        Guid requestId,
        Guid patientProfileId,
        string codeHash,
        DateTime approvedAt,
        DateTime codeExpiresAt,
        DateTime pendingExpiresBefore,
        CancellationToken cancellationToken);

    Task<int> RejectPendingAsync(
        Guid requestId,
        Guid patientProfileId,
        DateTime rejectedAt,
        DateTime pendingExpiresBefore,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PatientAccessRequest>> GetCodeCandidatesAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        CancellationToken cancellationToken);

    Task<int> ExpireStaleActiveAccessAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<int> RedeemApprovedAsync(
        Guid requestId,
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime redeemedAt,
        CancellationToken cancellationToken);

    Task<int> ExpireApprovedAsync(
        Guid requestId,
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    void AddAccess(DoctorPatientAccess access);
}
