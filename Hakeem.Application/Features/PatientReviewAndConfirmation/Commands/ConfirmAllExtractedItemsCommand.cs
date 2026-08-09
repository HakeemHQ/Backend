using MediatR;

namespace Hakeem.Application.Features.PatientReviewAndConfirmation.Commands;

public sealed record ConfirmAllExtractedItemsCommand(Guid DocumentId)
    : IRequest<ConfirmAllExtractedItemsResult>;

public sealed record ConfirmAllExtractedItemsResult(
    Guid DocumentId,
    int ConfirmedItemCount,
    int SkippedAlreadyConfirmedItemCount,
    IReadOnlyList<ReviewExtractedItemResult> Items);
