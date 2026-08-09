using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;

public sealed record GetMedicalCvPdfQuery(Guid MedicalCvVersionId)
    : IRequest<GetMedicalCvPdfResult>;

public sealed record GetMedicalCvPdfResult(Stream Content);
