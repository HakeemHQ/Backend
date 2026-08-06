using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Rag;

public interface IMedicalRecordFieldIndexOutbox : IScoped
{
    void EnqueueIndexing(Guid medicalRecordFieldId);

    void EnqueueIndexing(IEnumerable<Guid> medicalRecordFieldIds);
}
