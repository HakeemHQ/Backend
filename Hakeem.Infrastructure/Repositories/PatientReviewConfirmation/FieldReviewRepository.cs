using Hakeem.Application.Repositories.PatientReviewConfirmation;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Infrastructure.Repositories.PatientReviewConfirmation
{
    public class FieldReviewRepository : IFieldReviewRepository,IScoped
    {
        private readonly ApplicationDbContext _context;

        public FieldReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(FieldReview review)
        {
            _context.FieldReviews.Add(review);
        }

        public Task<FieldReview?> GetByExtractedFieldIdAsync(Guid extractedFieldId,CancellationToken cancellationToken)
        {
            return _context.FieldReviews.FirstOrDefaultAsync(x => x.ExtractedFieldId == extractedFieldId,cancellationToken);
        }
    }
}
