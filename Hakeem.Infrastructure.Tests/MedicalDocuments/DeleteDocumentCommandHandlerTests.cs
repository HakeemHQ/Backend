using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalDocuments.Commands.DeleteDocument;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Services.Access;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Reviews;
using Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Infrastructure.Tests.MedicalDocuments;

public sealed class DeleteDocumentCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotReviewedDocument_DeletesDatabaseRecordAndFile()
    {
        var document = new MedicalDocument
        {
            Id = Guid.NewGuid(),
            PatientProfileId = Guid.NewGuid(),
            FilePath = "documents/example.pdf"
        };
        var repository = new FakeMedicalDocumentRepository(document);
        var fileStorage = new FakeDocumentFileStorage();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, fileStorage, unitOfWork);

        await handler.Handle(
            new DeleteDocumentCommand(document.Id),
            CancellationToken.None);

        Assert.Same(document, repository.RemovedDocument);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(document.FilePath, fileStorage.DeletedFilePath);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    public async Task Handle_ReviewedDocument_ReturnsConflictAndDoesNotDelete(
        int reviewedItemCount,
        int totalItemCount)
    {
        var document = CreateReviewedDocument(reviewedItemCount, totalItemCount);
        var repository = new FakeMedicalDocumentRepository(document);
        var fileStorage = new FakeDocumentFileStorage();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, fileStorage, unitOfWork);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new DeleteDocumentCommand(document.Id),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.DocumentAlreadyReviewedCannotDelete, exception.ErrorCode);
        Assert.Null(repository.RemovedDocument);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Null(fileStorage.DeletedFilePath);
    }

    private static DeleteDocumentCommandHandler CreateHandler(
        FakeMedicalDocumentRepository repository,
        FakeDocumentFileStorage fileStorage,
        FakeUnitOfWork unitOfWork) =>
        new(
            repository,
            new FakeDoctorPatientAccessGuard(),
            fileStorage,
            unitOfWork);

    private static MedicalDocument CreateReviewedDocument(
        int reviewedItemCount,
        int totalItemCount)
    {
        var document = new MedicalDocument
        {
            Id = Guid.NewGuid(),
            PatientProfileId = Guid.NewGuid(),
            FilePath = "documents/example.pdf"
        };

        for (var index = 0; index < totalItemCount; index++)
        {
            document.ExtractedItems.Add(new ExtractedItem
            {
                ReviewStatus = index < reviewedItemCount
                    ? ExtractedItemReviewStatus.Reviewed
                    : ExtractedItemReviewStatus.NotReviewed
            });
        }

        document.RefreshReviewStatus();
        return document;
    }

    private sealed class FakeDoctorPatientAccessGuard : IDoctorPatientAccessGuard
    {
        public Task<DoctorProfile> RequireDoctorWithActiveAccessAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DoctorProfile());
    }

    private sealed class FakeDocumentFileStorage : IDocumentFileStorage
    {
        public string? DeletedFilePath { get; private set; }

        public Task<string> SaveAsync(
            IFormFile file,
            Guid documentId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            DeletedFilePath = filePath;
            return Task.FromResult(true);
        }
    }
}
