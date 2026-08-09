namespace Hakeem.Application.Interfaces.Processors;

public interface IMedicalCvGenerationProcessor
{
    Task ProcessAsync(
        Guid medicalCvVersionId,
        string language,
        CancellationToken cancellationToken);
}
