using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Infrastructure.Configurations
{
    public class MedicalRecordFieldConfiguration : IEntityTypeConfiguration<MedicalRecordField>
    {
        public void Configure(EntityTypeBuilder<MedicalRecordField> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FieldName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Value)
                   .IsRequired();

            builder.HasOne(x => x.MedicalRecord)
                   .WithMany(x => x.Fields)
                   .HasForeignKey(x => x.MedicalRecordId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.MedicalRecordId,
                x.FieldName
            })
            .IsUnique();
        }
    }

}
