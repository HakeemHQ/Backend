namespace Hakeem.Domain.Entities
{
    public class MedicalRecordField:BaseEntity
    {
        public Guid MedicalRecordId { get; set; }

        public Guid? SourceExtractedFieldId { get; set; }

        public string FieldName { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;

        public virtual ExtractedField? SourceExtractedField { get; set; }
    }
}