using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Rag;

public interface IMedicalRecordIndexOutbox : IScoped
{
    void EnqueueIndexing(Guid medicalRecordId);
}
