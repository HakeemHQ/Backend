using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;

public sealed record GetMedicalCvPdfQuery(
    Guid MedicalCvId,
    Guid MedicalCvVersionId)
    : IRequest<GetMedicalCvPdfResult>;

public sealed record GetMedicalCvPdfResult(Stream Content);
