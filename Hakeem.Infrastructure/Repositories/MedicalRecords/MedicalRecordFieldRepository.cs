using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.MedicalRecords;

public sealed class MedicalRecordFieldRepository(ApplicationDbContext dbContext)
    : IMedicalRecordFieldRepository
{
    public Task<MedicalRecordField?> GetByIdWithRecordAsync(
        Guid medicalRecordFieldId,
        CancellationToken cancellationToken)
    {
        return dbContext.MedicalRecordFields
            .AsNoTracking()
            .Include(field => field.MedicalRecord)
            .SingleOrDefaultAsync(
                field => field.Id == medicalRecordFieldId,
                cancellationToken);
    }
}
