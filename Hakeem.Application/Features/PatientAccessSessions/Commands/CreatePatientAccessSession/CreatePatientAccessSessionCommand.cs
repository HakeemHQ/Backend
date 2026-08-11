using MediatR;

namespace Hakeem.Application.Features.PatientAccessSessions.Commands.CreatePatientAccessSession;

public sealed class CreatePatientAccessSessionCommand
    : IRequest<CreatePatientAccessSessionResult>
{
    public string PatientCode { get; set; } = string.Empty;
    public string OneTimeCode { get; set; } = string.Empty;
}

public sealed record CreatePatientAccessSessionResult(
    Guid AccessId,
    Guid PatientId,
    PatientAccessSessionPatient Patient,
    DateTime GrantedAt,
    DateTime ExpiresAt);

public sealed record PatientAccessSessionPatient(
    string PatientCode,
    string FullName,
    string BirthDate);
