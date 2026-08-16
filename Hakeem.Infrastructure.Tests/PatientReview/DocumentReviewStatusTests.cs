using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Enums.Reviews;

namespace Hakeem.Infrastructure.Tests.PatientReview;

public sealed class DocumentReviewStatusTests
{
    [Fact]
    public void RefreshReviewStatus_WithNoReviewedItems_SetsNotReviewed()
    {
        var document = CreateDocument(
            ExtractedItemReviewStatus.NotReviewed,
            ExtractedItemReviewStatus.NotReviewed);

        document.RefreshReviewStatus();

        Assert.Equal(DocumentReviewStatus.NotReviewed, document.ReviewStatus);
    }

    [Fact]
    public void RefreshReviewStatus_WithSomeReviewedItems_SetsPartiallyReviewed()
    {
        var document = CreateDocument(
            ExtractedItemReviewStatus.Reviewed,
            ExtractedItemReviewStatus.NotReviewed);

        document.RefreshReviewStatus();

        Assert.Equal(DocumentReviewStatus.PartiallyReviewed, document.ReviewStatus);
    }

    [Fact]
    public void RefreshReviewStatus_WithAllItemsReviewed_SetsFullyReviewed()
    {
        var document = CreateDocument(
            ExtractedItemReviewStatus.Reviewed,
            ExtractedItemReviewStatus.Reviewed);

        document.RefreshReviewStatus();

        Assert.Equal(DocumentReviewStatus.FullyReviewed, document.ReviewStatus);
    }

    [Fact]
    public void RefreshReviewStatus_WithNoExtractedItems_SetsNotReviewed()
    {
        var document = new MedicalDocument();

        document.RefreshReviewStatus();

        Assert.Equal(DocumentReviewStatus.NotReviewed, document.ReviewStatus);
    }

    [Fact]
    public void CompleteExtraction_ResetsReviewStatusForNewlyExtractedItems()
    {
        var document = CreateDocument(ExtractedItemReviewStatus.Reviewed);
        document.RefreshReviewStatus();
        document.QueueExtraction();
        document.StartExtraction();

        document.CompleteExtraction();

        Assert.Equal(DocumentReviewStatus.NotReviewed, document.ReviewStatus);
    }

    private static MedicalDocument CreateDocument(
        params ExtractedItemReviewStatus[] statuses)
    {
        var document = new MedicalDocument();

        foreach (var status in statuses)
        {
            document.ExtractedItems.Add(new ExtractedItem
            {
                ReviewStatus = status
            });
        }

        return document;
    }
}
