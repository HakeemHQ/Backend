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
