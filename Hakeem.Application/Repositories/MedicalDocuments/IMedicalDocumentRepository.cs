using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.MedicalDocuments;

public interface IMedicalDocumentRepository : IScoped
{
    void Add(MedicalDocument medicalDocument);
}
