using System;
using System.Collections.Generic;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Enums.Reviews;

namespace Hakeem.Domain.Entities;

public class MedicalDocument : BaseEntity
{
    public const string UnclassifiedDocumentType = "Unclassified";

    public Guid PatientProfileId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public ExtractionStatus ExtractionStatus { get; private set; } = ExtractionStatus.Queued;
    public DocumentReviewStatus ReviewStatus { get; private set; } = DocumentReviewStatus.NotReviewed;
    public string? FailureCode { get; private set; }

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<ExtractedItem> ExtractedItems { get; set; } = new List<ExtractedItem>();
    public virtual ICollection<SourceReference> SourceReferences { get; set; } = new List<SourceReference>();
    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

    public void QueueExtraction()
    {
        if (ExtractionStatus == ExtractionStatus.Processing)
        {
            throw new InvalidOperationException("A processing extraction cannot be queued.");
        }

        ExtractionStatus = ExtractionStatus.Queued;
        FailureCode = null;
    }

    public void StartExtraction()
    {
        EnsureExtractionStatus(ExtractionStatus.Queued);
        ExtractionStatus = ExtractionStatus.Processing;
        FailureCode = null;
    }

    public void CompleteExtraction()
    {
        EnsureExtractionStatus(ExtractionStatus.Processing);
        ExtractionStatus = ExtractionStatus.Completed;
        ReviewStatus = DocumentReviewStatus.NotReviewed;
        FailureCode = null;
    }

    public void RefreshReviewStatus()
    {
        var itemCount = ExtractedItems.Count;
        var reviewedItemCount = ExtractedItems.Count(
            item => item.ReviewStatus == ExtractedItemReviewStatus.Reviewed);

        ReviewStatus = reviewedItemCount switch
        {
            0 => DocumentReviewStatus.NotReviewed,
            _ when reviewedItemCount == itemCount => DocumentReviewStatus.FullyReviewed,
            _ => DocumentReviewStatus.PartiallyReviewed
        };
    }

    public void FailExtraction(string failureCode)
    {
        EnsureExtractionStatus(ExtractionStatus.Processing);

        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new ArgumentException("A failure code is required.", nameof(failureCode));
        }

        ExtractionStatus = ExtractionStatus.Failed;
        FailureCode = failureCode.Trim();
    }

    public void RejectExtraction(string rejectionCode)
    {
        EnsureExtractionStatus(ExtractionStatus.Processing);

        if (string.IsNullOrWhiteSpace(rejectionCode))
        {
            throw new ArgumentException(
                "A rejection code is required.",
                nameof(rejectionCode));
        }

        ExtractionStatus = ExtractionStatus.Rejected;
        FailureCode = rejectionCode.Trim();
    }

    private void EnsureExtractionStatus(ExtractionStatus expectedStatus)
    {
        if (ExtractionStatus != expectedStatus)
        {
            throw new InvalidOperationException(
                $"Extraction must be in the {expectedStatus} state, but is currently {ExtractionStatus}.");
        }
    }
}
