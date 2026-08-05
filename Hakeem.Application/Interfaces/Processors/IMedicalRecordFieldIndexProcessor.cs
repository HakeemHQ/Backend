using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Processors;

public interface IMedicalRecordFieldIndexProcessor : IScoped
{
    Task ProcessAsync(
        Guid medicalRecordFieldId,
        CancellationToken cancellationToken);
}
