using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.MedicalRecords;

public interface IMedicalRecordFieldRepository : IScoped
{
    Task<MedicalRecordField?> GetByIdWithRecordAsync(
        Guid medicalRecordFieldId,
        CancellationToken cancellationToken);
}
