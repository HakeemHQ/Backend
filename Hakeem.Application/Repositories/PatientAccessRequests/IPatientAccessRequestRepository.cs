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
}
