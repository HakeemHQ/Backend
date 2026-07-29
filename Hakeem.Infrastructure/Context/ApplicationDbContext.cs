using Hakeem.Domain.Entities;
using Hakeem.Domain.Entities.NotificationsEntites;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Context;


public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public virtual DbSet<OutboxEvent> OutboxEvents { get; set; }
    public virtual DbSet<EmailRecipient> EmailRecipients { get; set; }
    public virtual DbSet<EmailTemplate> EmailTemplates { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<PatientProfile> PatientProfiles { get; set; }
    public virtual DbSet<MedicalDocument> MedicalDocuments { get; set; }
    public virtual DbSet<ExtractedItem> ExtractedItems { get; set; }
    public virtual DbSet<ExtractedField> ExtractedFields { get; set; }
    public virtual DbSet<FieldReview> FieldReviews { get; set; }
    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }
    public virtual DbSet<SourceReference> SourceReferences { get; set; }
    public virtual DbSet<DocumentChunk> DocumentChunks { get; set; }
    public virtual DbSet<MedicalCv> MedicalCvs { get; set; }
    public virtual DbSet<MedicalCvVersion> MedicalCvVersions { get; set; }
    public virtual DbSet<Reminder> Reminders { get; set; }
    public virtual DbSet<ConsentRecord> ConsentRecords { get; set; }
    public virtual DbSet<SharedCvLink> SharedCvLinks { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            if (entry.Entity is BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    baseEntity.CreatedAt = now;
                }
                baseEntity.UpdatedAt = now;
            }
        }
    }
}


