using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;

namespace Hakeem.Infrastructure.Repositories.MedicalDocuments;

public sealed class MedicalDocumentRepository(ApplicationDbContext dbContext)
    : IMedicalDocumentRepository
{
    public void Add(MedicalDocument medicalDocument)
    {
        dbContext.MedicalDocuments.Add(medicalDocument);
    }
}
