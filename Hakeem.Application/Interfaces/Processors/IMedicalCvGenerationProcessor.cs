namespace Hakeem.Application.Interfaces.Processors;

public interface IMedicalCvGenerationProcessor
{
    Task ProcessAsync(
        Guid medicalCvVersionId,
        CancellationToken cancellationToken);
}
