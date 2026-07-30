namespace Hakeem.Application.Interfaces.Processors;

public interface IDocumentExtractionProcessor
{
    Task ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken);
}
