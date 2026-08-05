using Hakeem.Application.Repositories.PatientReviewConfirmation;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;

namespace Hakeem.Infrastructure.Repositories.PatientReviewConfirmation
{
    public class SourceReferenceRepository : ISourceReferenceRepository, IScoped
    {
        private readonly ApplicationDbContext _context;

        public SourceReferenceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(SourceReference sourceReference)
        {
            _context.SourceReferences.Add(sourceReference);
        }
    }

}
