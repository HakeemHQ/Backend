using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Processors;

public interface IMedicalRecordIndexProcessor : IScoped
{
    Task ProcessAsync(
        Guid medicalRecordId,
        CancellationToken cancellationToken);
}
