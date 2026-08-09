using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvs;

public sealed record GetMedicalCvsQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20)
    : IRequest<GetMedicalCvsResponse>;
