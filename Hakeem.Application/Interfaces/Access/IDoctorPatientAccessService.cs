using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Access;

public interface IDoctorPatientAccessService : IScoped
{
    Task<bool> HasActiveAccessAsync(
        Guid doctorId,
        Guid patientId,
        CancellationToken cancellationToken);
}
