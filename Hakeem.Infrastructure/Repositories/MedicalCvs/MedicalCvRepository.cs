using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.MedicalCvs;

public sealed class MedicalCvRepository(ApplicationDbContext context)
    : IMedicalCvRepository, IScoped
{
    public Task<MedicalCv?> GetByLogicalIdentityAsync(
        Guid patientId,
        MedicalCvScopeType scopeType,
        string? focus,
        CancellationToken cancellationToken)
    {
        return context.MedicalCvs.SingleOrDefaultAsync(
            medicalCv =>
                medicalCv.PatientId == patientId &&
                medicalCv.ScopeType == scopeType &&
                medicalCv.Focus == focus,
            cancellationToken);
    }

    public async Task<int> GetNextVersionNumberAsync(
        Guid medicalCvId,
        CancellationToken cancellationToken)
    {
        var latestVersion = await context.MedicalCvVersions
            .Where(version => version.MedicalCvId == medicalCvId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken);

        return latestVersion.GetValueOrDefault() + 1;
    }

    public void Add(MedicalCv medicalCv)
    {
        context.MedicalCvs.Add(medicalCv);
    }

    public void AddVersion(MedicalCvVersion version)
    {
        context.MedicalCvVersions.Add(version);
    }
}
