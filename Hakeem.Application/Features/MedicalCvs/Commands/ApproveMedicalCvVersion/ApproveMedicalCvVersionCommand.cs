using Hakeem.Domain.Enums.MedicalCvs;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Commands.ApproveMedicalCvVersion;

public sealed record ApproveMedicalCvVersionCommand(Guid MedicalCvVersionId)
    : IRequest<ApproveMedicalCvVersionResponse>;

public sealed record ApproveMedicalCvVersionResponse(
    Guid MedicalCvVersionId,
    int VersionNumber,
    MedicalCvVersionStatus Status,
    DateTime ApprovedAt);
