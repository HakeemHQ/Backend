using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Identity;

public interface IPatientCodeGenerator : IScoped
{
    Task<string> GenerateUniqueAsync(CancellationToken cancellationToken);
}
