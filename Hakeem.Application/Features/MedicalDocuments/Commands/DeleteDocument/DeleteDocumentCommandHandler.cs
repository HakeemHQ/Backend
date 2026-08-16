using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Services.Access;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Features.MedicalDocuments.Commands.DeleteDocument;

public sealed class DeleteDocumentCommandHandler(
    IMedicalDocumentRepository medicalDocumentRepository,
    IDoctorPatientAccessGuard doctorPatientAccessGuard,
    IDocumentFileStorage documentFileStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteDocumentCommand>
{
    public async Task Handle(
        DeleteDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await medicalDocumentRepository.GetByIdAsync(
            request.DocumentId,
            cancellationToken);

        if (document is null)
        {
            throw new NotFoundException(ErrorCodes.DocumentNotFound);
        }

        await doctorPatientAccessGuard.RequireDoctorWithActiveAccessAsync(
            document.PatientProfileId,
            cancellationToken);

        if (document.ReviewStatus != DocumentReviewStatus.NotReviewed)
        {
            throw new ConflictException(
                ErrorCodes.DocumentAlreadyReviewedCannotDelete);
        }

        if (await medicalDocumentRepository.HasSourceReferencesAsync(
                document.Id,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.DocumentHasConfirmedReferences);
        }

        var filePath = document.FilePath;
        medicalDocumentRepository.Remove(document);
        await unitOfWork.SaveChanges(cancellationToken);

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            await documentFileStorage.DeleteAsync(filePath, cancellationToken);
        }
    }
}
