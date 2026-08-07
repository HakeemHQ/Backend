$entitiesDir = "d:\ITI\GP\Backend\Hakeem.Domain\Entities"
$configsDir = "d:\ITI\GP\Backend\Hakeem.Infrastructure\Configurations"
$medicalCvEnumsDir = "d:\ITI\GP\Backend\Hakeem.Domain\Enums\MedicalCvs"

if (!(Test-Path -Path $entitiesDir)) { New-Item -ItemType Directory -Path $entitiesDir | Out-Null }
if (!(Test-Path -Path $configsDir)) { New-Item -ItemType Directory -Path $configsDir | Out-Null }
if (!(Test-Path -Path $medicalCvEnumsDir)) { New-Item -ItemType Directory -Path $medicalCvEnumsDir | Out-Null }

$medicalCvEnums = @{
    "MedicalCvScopeType" = @"
namespace Hakeem.Domain.Enums.MedicalCvs;

public enum MedicalCvScopeType
{
    Full,
    Focused
}
"@;

    "MedicalCvVersionStatus" = @"
namespace Hakeem.Domain.Enums.MedicalCvs;

public enum MedicalCvVersionStatus
{
    Draft,
    Approved
}
"@;
}

$entities = @{
    "User" = @"
using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public virtual PatientProfile? PatientProfile { get; set; }
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
"@;

    "PatientProfile" = @"
using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class PatientProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual ICollection<MedicalDocument> MedicalDocuments { get; set; } = new List<MedicalDocument>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<MedicalCv> MedicalCvs { get; set; } = new List<MedicalCv>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public virtual ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
"@;

    "MedicalDocument" = @"
using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class MedicalDocument : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<ExtractedField> ExtractedFields { get; set; } = new List<ExtractedField>();
    public virtual ICollection<SourceReference> SourceReferences { get; set; } = new List<SourceReference>();
    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();
}
"@;

    "ExtractedField" = @"
using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class ExtractedField : BaseEntity
{
    public Guid DocumentId { get; set; }
    public string FieldGroup { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string ExtractedValue { get; set; } = string.Empty;
    public decimal Confidence { get; set; }

    public virtual MedicalDocument MedicalDocument { get; set; } = null!;
    public virtual FieldReview? FieldReview { get; set; }
}
"@;

    "FieldReview" = @"
using System;

namespace Hakeem.Domain.Entities;

public class FieldReview : BaseEntity
{
    public Guid ExtractedFieldId { get; set; }
    public Guid MedicalRecordId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string CorrectedValue { get; set; } = string.Empty;
    public DateTime ReviewedAt { get; set; }

    public virtual ExtractedField ExtractedField { get; set; } = null!;
    public virtual MedicalRecord MedicalRecord { get; set; } = null!;
}
"@;

    "MedicalRecord" = @"
using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class MedicalRecord : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public string RecordType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime ClinicalDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<FieldReview> FieldReviews { get; set; } = new List<FieldReview>();
    public virtual ICollection<SourceReference> SourceReferences { get; set; } = new List<SourceReference>();
    public virtual ICollection<MedicalCvVersion> SummarizedInCvVersions { get; set; } = new List<MedicalCvVersion>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
"@;

    "SourceReference" = @"
using System;

namespace Hakeem.Domain.Entities;

public class SourceReference : BaseEntity
{
    public Guid MedicalRecordId { get; set; }
    public Guid DocumentId { get; set; }
    public string PageReference { get; set; } = string.Empty;

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    public virtual MedicalDocument MedicalDocument { get; set; } = null!;
}
"@;

    "DocumentChunk" = @"
using System;

namespace Hakeem.Domain.Entities;

public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int SequenceNumber { get; set; }
    public string PageReference { get; set; } = string.Empty;

    public virtual MedicalDocument MedicalDocument { get; set; } = null!;
}
"@;

    "MedicalCv" = @"
using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class MedicalCv : BaseEntity
{
    public Guid PatientId { get; set; }
    public string Title { get; set; } = string.Empty;

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<MedicalCvVersion> Versions { get; set; } = new List<MedicalCvVersion>();
}
"@;

    "MedicalCvVersion" = @"
using System;
using System.Collections.Generic;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Domain.Entities;

public class MedicalCvVersion : BaseEntity
{
    public Guid MedicalCvId { get; set; }
    public int VersionNumber { get; set; }
    public MedicalCvScopeType ScopeType { get; set; }
    public string? Focus { get; set; }
    public MedicalCvVersionStatus Status { get; set; } = MedicalCvVersionStatus.Draft;
    public string PdfFileKey { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }

    public virtual MedicalCv MedicalCv { get; set; } = null!;
    public virtual ICollection<MedicalRecord> SummarizedRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<SharedCvLink> SharedCvLinks { get; set; } = new List<SharedCvLink>();
}
"@;

    "Reminder" = @"
using System;

namespace Hakeem.Domain.Entities;

public class Reminder : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public Guid? MedicalRecordId { get; set; }
    public string ReminderType { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public string RepeatPattern { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual MedicalRecord? MedicalRecord { get; set; }
}
"@;

    "ConsentRecord" = @"
using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class ConsentRecord : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public DateTime EffectiveAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<SharedCvLink> SharedCvLinks { get; set; } = new List<SharedCvLink>();
}
"@;

    "SharedCvLink" = @"
using System;

namespace Hakeem.Domain.Entities;

public class SharedCvLink : BaseEntity
{
    public Guid MedicalCvVersionId { get; set; }
    public Guid ConsentRecordId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public virtual MedicalCvVersion MedicalCvVersion { get; set; } = null!;
    public virtual ConsentRecord ConsentRecord { get; set; } = null!;
}
"@;

    "AuditLog" = @"
using System;

namespace Hakeem.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public Guid? PatientProfileId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }

    public virtual User? ActorUser { get; set; }
    public virtual PatientProfile? PatientProfile { get; set; }
}
"@;

    "RefreshToken" = @"
using System;

namespace Hakeem.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime AddedDate { get; set; }
    public DateTime ExpiryDate { get; set; }

    public virtual User User { get; set; } = null!;
}
"@;
}

$configs = @{
    "UserConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(u => u.PatientProfile)
               .WithOne(p => p.User)
               .HasForeignKey<PatientProfile>(p => p.UserId);

        builder.HasMany(u => u.AuditLogs)
               .WithOne(a => a.ActorUser)
               .HasForeignKey(a => a.ActorUserId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.RefreshTokens)
               .WithOne(r => r.User)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
"@;

    "PatientProfileConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasMany(p => p.MedicalDocuments)
               .WithOne(d => d.PatientProfile)
               .HasForeignKey(d => d.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.MedicalRecords)
               .WithOne(m => m.PatientProfile)
               .HasForeignKey(m => m.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.MedicalCvs)
               .WithOne(c => c.PatientProfile)
               .HasForeignKey(c => c.PatientId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Reminders)
               .WithOne(r => r.PatientProfile)
               .HasForeignKey(r => r.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ConsentRecords)
               .WithOne(c => c.PatientProfile)
               .HasForeignKey(c => c.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.AuditLogs)
               .WithOne(a => a.PatientProfile)
               .HasForeignKey(a => a.PatientProfileId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
"@;

    "MedicalDocumentConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalDocumentConfiguration : IEntityTypeConfiguration<MedicalDocument>
{
    public void Configure(EntityTypeBuilder<MedicalDocument> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasMany(d => d.ExtractedFields)
               .WithOne(e => e.MedicalDocument)
               .HasForeignKey(e => e.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.SourceReferences)
               .WithOne(s => s.MedicalDocument)
               .HasForeignKey(s => s.DocumentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.DocumentChunks)
               .WithOne(c => c.MedicalDocument)
               .HasForeignKey(c => c.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
"@;

    "ExtractedFieldConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class ExtractedFieldConfiguration : IEntityTypeConfiguration<ExtractedField>
{
    public void Configure(EntityTypeBuilder<ExtractedField> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.FieldReview)
               .WithOne(f => f.ExtractedField)
               .HasForeignKey<FieldReview>(f => f.ExtractedFieldId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(e => e.Confidence).HasColumnType("decimal(18,4)");
    }
}
"@;

    "FieldReviewConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class FieldReviewConfiguration : IEntityTypeConfiguration<FieldReview>
{
    public void Configure(EntityTypeBuilder<FieldReview> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
"@;

    "MedicalRecordConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasMany(m => m.FieldReviews)
               .WithOne(f => f.MedicalRecord)
               .HasForeignKey(f => f.MedicalRecordId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.SourceReferences)
               .WithOne(s => s.MedicalRecord)
               .HasForeignKey(s => s.MedicalRecordId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.SummarizedInCvVersions)
               .WithMany(v => v.SummarizedRecords)
               .UsingEntity(j => j.ToTable("MedicalRecordMedicalCvVersion"));

        builder.HasMany(m => m.Reminders)
               .WithOne(r => r.MedicalRecord)
               .HasForeignKey(r => r.MedicalRecordId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
"@;

    "SourceReferenceConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class SourceReferenceConfiguration : IEntityTypeConfiguration<SourceReference>
{
    public void Configure(EntityTypeBuilder<SourceReference> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
"@;

    "DocumentChunkConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
"@;

    "MedicalCvConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalCvConfiguration : IEntityTypeConfiguration<MedicalCv>
{
    public void Configure(EntityTypeBuilder<MedicalCv> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
               .HasMaxLength(200)
               .IsRequired();

        builder.HasMany(m => m.Versions)
               .WithOne(v => v.MedicalCv)
               .HasForeignKey(v => v.MedicalCvId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
"@;

    "MedicalCvVersionConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalCvVersionConfiguration : IEntityTypeConfiguration<MedicalCvVersion>
{
    public void Configure(EntityTypeBuilder<MedicalCvVersion> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.MedicalCvId, e.VersionNumber })
               .IsUnique();

        builder.Property(e => e.ScopeType)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(e => e.Focus)
               .HasMaxLength(200);

        builder.Property(e => e.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(e => e.PdfFileKey)
               .HasMaxLength(500)
               .IsRequired();

        builder.HasMany(m => m.SharedCvLinks)
               .WithOne(s => s.MedicalCvVersion)
               .HasForeignKey(s => s.MedicalCvVersionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
"@;

    "ReminderConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
"@;

    "ConsentRecordConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasMany(c => c.SharedCvLinks)
               .WithOne(s => s.ConsentRecord)
               .HasForeignKey(s => s.ConsentRecordId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
"@;

    "SharedCvLinkConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class SharedCvLinkConfiguration : IEntityTypeConfiguration<SharedCvLink>
{
    public void Configure(EntityTypeBuilder<SharedCvLink> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
"@;

    "AuditLogConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
"@;

    "RefreshTokenConfiguration" = @"
using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
"@;
}

foreach ($key in $entities.Keys) {
    $filePath = Join-Path -Path $entitiesDir -ChildPath "$key.cs"
    Set-Content -Path $filePath -Value $entities[$key] -Encoding UTF8
}

foreach ($key in $medicalCvEnums.Keys) {
    $filePath = Join-Path -Path $medicalCvEnumsDir -ChildPath "$key.cs"
    Set-Content -Path $filePath -Value $medicalCvEnums[$key] -Encoding UTF8
}

foreach ($key in $configs.Keys) {
    $filePath = Join-Path -Path $configsDir -ChildPath "$key.cs"
    Set-Content -Path $filePath -Value $configs[$key] -Encoding UTF8
}

Write-Output "Entities and Configurations generated successfully."
