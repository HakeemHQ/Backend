using MediatR;

namespace Hakeem.Application.Features.PatientIdentities.Commands.VerifyPatientIdentity;

public sealed class VerifyPatientIdentityCommand : IRequest<VerifyPatientIdentityResult>
{
    public string PatientCode { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
}

public sealed record VerifyPatientIdentityResult(
    Guid PatientId,
    string PatientCode,
    string FullName,
    string IdentityVerificationStatus,
    bool ClaimCorrected,
    DateTime VerifiedAt);
