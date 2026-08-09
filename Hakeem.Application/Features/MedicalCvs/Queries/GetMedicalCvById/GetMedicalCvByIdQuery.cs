using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvById;

public sealed record GetMedicalCvByIdQuery(Guid MedicalCvId)
    : IRequest<GetMedicalCvByIdResponse>;
