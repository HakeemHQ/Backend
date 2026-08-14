using Hakeem.Domain.Enums.Identity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.Commands.UpdateDoctorStatus
{
    public sealed record UpdateDoctorStatusCommand(
    Guid DoctorId,
    AccountStatus Status
) : IRequest;
}
