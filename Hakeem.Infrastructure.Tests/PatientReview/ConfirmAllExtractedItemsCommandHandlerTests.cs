using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Features.PatientReviewAndConfirmation.Commands;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Application.Services.Access;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Reviews;
using Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;
using MediatR;

namespace Hakeem.Infrastructure.Tests.PatientReview;

public sealed class ConfirmAllExtractedItemsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ApprovesEveryFieldAndSkipsConfirmedItems()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        var document = CreateCompletedDocument(patient.Id);
        var pendingItem = CreateItem(
            document.Id,
            ExtractedItemReviewStatus.Pending,
            "LabTestName",
            "LabValue");
        var confirmedItem = CreateItem(
            document.Id,
            ExtractedItemReviewStatus.Confirmed,
            "PatientName");
        var itemHandler = new FakeItemConfirmationHandler();
        var handler = new ConfirmAllExtractedItemsCommandHandler(
            new FakeDoctorPatientAccessGuard(),
            new FakeMedicalDocumentRepository(
                document,
                [pendingItem, confirmedItem]),
            itemHandler);

        var result = await handler.Handle(
            new ConfirmAllExtractedItemsCommand(document.Id),
            CancellationToken.None);

        Assert.Equal(document.Id, result.DocumentId);
        Assert.Equal(1, result.ConfirmedItemCount);
        Assert.Equal(1, result.SkippedAlreadyConfirmedItemCount);
        Assert.Single(result.Items);
        var received = Assert.Single(itemHandler.ReceivedCommands);
        Assert.Equal(pendingItem.Id, received.ExtractedItemId);
        Assert.Equal(2, received.Fields.Count);
        Assert.All(
            received.Fields,
            field => Assert.Equal(
                FieldReviewDecision.Approved,
                field.Decision));
    }

    [Fact]
    public async Task Handle_AllItemsAlreadyConfirmed_ReturnsOnlySkippedCount()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        var document = CreateCompletedDocument(patient.Id);
        var confirmedItem = CreateItem(
            document.Id,
            ExtractedItemReviewStatus.Confirmed,
            "MedicationName");
        var itemHandler = new FakeItemConfirmationHandler();
        var handler = new ConfirmAllExtractedItemsCommandHandler(
            new FakeDoctorPatientAccessGuard(),
            new FakeMedicalDocumentRepository(document, [confirmedItem]),
            itemHandler);

        var result = await handler.Handle(
            new ConfirmAllExtractedItemsCommand(document.Id),
            CancellationToken.None);

        Assert.Equal(0, result.ConfirmedItemCount);
        Assert.Equal(1, result.SkippedAlreadyConfirmedItemCount);
        Assert.Empty(result.Items);
        Assert.Empty(itemHandler.ReceivedCommands);
    }

    private static MedicalDocument CreateCompletedDocument(Guid patientId)
    {
        var document = new MedicalDocument
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId
        };
        document.StartExtraction();
        document.CompleteExtraction();
        return document;
    }

    private static ExtractedItem CreateItem(
        Guid documentId,
        ExtractedItemReviewStatus reviewStatus,
        params string[] fieldNames)
    {
        var item = new ExtractedItem
        {
            Id = Guid.NewGuid(),
            MedicalDocumentId = documentId,
            ItemType = "LabResult",
            SequenceNumber = 1,
            PageNumber = 1,
            ReviewStatus = reviewStatus
        };

        foreach (var fieldName in fieldNames)
        {
            item.ExtractedFields.Add(new ExtractedField
            {
                Id = Guid.NewGuid(),
                ExtractedItemId = item.Id,
                FieldName = fieldName,
                ExtractedValue = "Example"
            });
        }

        return item;
    }

    private sealed record FakeCurrentUserContext(Guid UserId)
        : ICurrentUserContext;

    private sealed class FakePatientProfileRepository(PatientProfile patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient.Id == patientProfileId ? patient : null);

        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient.UserId == userId ? patient : null);

        public Task<PatientProfile?> UpdateByUserIdAsync(
            Guid userId,
            string? fullName,
            DateTime? birthDate,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            string? gender,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeDoctorPatientAccessGuard : IDoctorPatientAccessGuard
    {
        public Task<DoctorProfile> RequireDoctorWithActiveAccessAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            });
    }

    private sealed class FakeItemConfirmationHandler
        : IRequestHandler<
            PatientReviewConfirmationCommand,
            ReviewExtractedItemResult>
    {
        public List<PatientReviewConfirmationCommand> ReceivedCommands { get; } = [];

        public Task<ReviewExtractedItemResult> Handle(
            PatientReviewConfirmationCommand request,
            CancellationToken cancellationToken)
        {
            ReceivedCommands.Add(request);
            return Task.FromResult(new ReviewExtractedItemResult(
                request.ExtractedItemId,
                "LabResult",
                ExtractedItemReviewStatus.Confirmed.ToString(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                []));
        }
    }
}
