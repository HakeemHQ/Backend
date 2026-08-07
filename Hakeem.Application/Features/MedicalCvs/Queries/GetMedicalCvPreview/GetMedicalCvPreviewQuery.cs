using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPreview;

public sealed record GetMedicalCvPreviewQuery(
    Guid MedicalCvId,
    Guid MedicalCvVersionId,
    string Token)
    : IRequest<GetMedicalCvPdfResult>;
