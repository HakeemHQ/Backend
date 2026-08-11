using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.PatientAccessRequests;

public interface IPatientAccessRequestRepository : IScoped
{
    Task<PatientProfile?> GetPatientByCodeAsync(
        string patientCode,
        CancellationToken cancellationToken);

    Task<bool> HasPendingRequestAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveAccessAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    void Add(PatientAccessRequest accessRequest);

    Task<IReadOnlyList<PatientAccessRequest>> GetForPatientAsync(
        Guid patientProfileId,
        Hakeem.Domain.Enums.Access.PatientAccessRequestStatus? status,
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
        CancellationToken cancellationToken);

    Task<int> RejectPendingAsync(
        Guid requestId,
        Guid patientProfileId,
        DateTime rejectedAt,
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

    void AddAccess(DoctorPatientAccess access);
}
