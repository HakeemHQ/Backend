using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Commands.CreateMedicalCvPreviewLink;

public sealed record CreateMedicalCvPreviewLinkCommand(
    Guid MedicalCvId,
    Guid MedicalCvVersionId)
    : IRequest<CreateMedicalCvPreviewLinkResponse>;

public sealed record CreateMedicalCvPreviewLinkResponse(
    string PdfUrl,
    DateTimeOffset PreviewExpiresAt);
