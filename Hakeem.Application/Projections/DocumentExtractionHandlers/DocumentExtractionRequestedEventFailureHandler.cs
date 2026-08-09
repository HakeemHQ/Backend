using Hakeem.Application.Abstractions;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Enums.Documents;

namespace Hakeem.Application.Projections.DocumentExtractionHandlers;

public sealed class DocumentExtractionRequestedEventFailureHandler(
    IMedicalDocumentRepository medicalDocumentRepository)
    : IOutboxEventFailureHandler<DocumentExtractionRequestedEvent>
{
    public async Task HandleFailureAsync(
        DocumentExtractionRequestedEvent @event,
        CancellationToken cancellationToken)
    {
        var document = await medicalDocumentRepository.GetByIdAsync(
            @event.DocumentId,
            cancellationToken);

        if (document?.ExtractionStatus == ExtractionStatus.Processing)
        {
            document.FailExtraction("Document.ExtractionFailed");
        }
    }
}
