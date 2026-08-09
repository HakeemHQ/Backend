using Hakeem.Application.Common;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.MedicalCvs;

public sealed class MedicalCvRepository(ApplicationDbContext context)
    : IMedicalCvRepository, IMedicalCvReadRepository, IScoped
{
    public Task<MedicalCvDetailReadModel?> GetDetailForPatientAsync(
        Guid medicalCvId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return context.MedicalCvs
            .AsNoTracking()
            .Where(medicalCv =>
                medicalCv.Id == medicalCvId &&
                medicalCv.PatientId == patientId)
            .Select(medicalCv => new MedicalCvDetailReadModel(
                medicalCv.Id,
                medicalCv.Title,
                medicalCv.ScopeType,
                medicalCv.Focus,
                medicalCv.CreatedAt,
                medicalCv.UpdatedAt,
                medicalCv.Versions
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => new MedicalCvVersionDetailReadModel(
                        version.Id,
                        version.VersionNumber,
                        version.Status,
                        version.CreatedAt,
                        version.ApprovedAt,
                        !string.IsNullOrEmpty(version.PdfFileKey)))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedResult<MedicalCvListReadModel>> GetForPatientAsync(
        Guid patientId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.MedicalCvs
            .AsNoTracking()
            .Where(medicalCv => medicalCv.PatientId == patientId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(medicalCv =>
                medicalCv.Title.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(medicalCv => medicalCv.UpdatedAt)
            .ThenBy(medicalCv => medicalCv.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(medicalCv => new MedicalCvListReadModel(
                medicalCv.Id,
                medicalCv.Title,
                medicalCv.ScopeType,
                medicalCv.Focus,
                medicalCv.Versions
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => new LatestMedicalCvVersionReadModel(
                        version.Id,
                        version.VersionNumber,
                        version.Status))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<MedicalCvListReadModel>(
            items,
            totalCount,
            page,
            pageSize);
    }

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

    public Task<MedicalCvVersion?> GetVersionForPatientAsync(
        Guid medicalCvVersionId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return context.MedicalCvVersions
            .Include(version => version.MedicalCv)
            .SingleOrDefaultAsync(
                version =>
                    version.Id == medicalCvVersionId &&
                    version.MedicalCv.PatientId == patientId,
                cancellationToken);
    }

    public Task<MedicalCvVersion?> GetVersionForGenerationAsync(
        Guid medicalCvVersionId,
        CancellationToken cancellationToken)
    {
        return context.MedicalCvVersions
            .Include(version => version.MedicalCv)
                .ThenInclude(medicalCv => medicalCv.PatientProfile)
                    .ThenInclude(patient => patient.User)
            .Include(version => version.SummarizedRecords)
            .SingleOrDefaultAsync(
                version => version.Id == medicalCvVersionId,
                cancellationToken);
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
