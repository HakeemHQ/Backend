using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Validation;

public interface IDocumentExtractionValidator : IScoped
{
    DocumentExtractionValidationResult Validate(
        DocumentExtractionResult? result);
}

public sealed record DocumentExtractionValidationResult(
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
