using Hakeem.Application.Features.Admin.Doctors.DTOs;
using MediatR;
namespace Hakeem.Application.Features.Admin.Doctors.Commands.AddDoctor
{
    public sealed record CreateDoctorCommand(
        string Email,
        string FullName,
        string Specialty,
        string TemporaryPassword)
        : IRequest<CreateDoctorResponse>;
}
