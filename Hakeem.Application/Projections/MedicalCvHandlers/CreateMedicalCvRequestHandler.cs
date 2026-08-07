using Hakeem.Application.Abstractions;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Projections.MedicalCvHandlers;

public sealed class CreateMedicalCvRequestHandler(
    IMedicalCvGenerationProcessor processor)
    : IOutboxEventHandler<CreateMedicalCvRequest>
{
    public Task HandleAsync(
        CreateMedicalCvRequest @event,
        CancellationToken cancellationToken)
    {
        return processor.ProcessAsync(
            @event.MedicalCvVersionId,
            cancellationToken);
    }
}
