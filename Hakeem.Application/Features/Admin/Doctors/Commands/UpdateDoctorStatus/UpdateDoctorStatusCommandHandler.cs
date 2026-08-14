using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.Commands.UpdateDoctorStatus
{
    public sealed class UpdateDoctorStatusCommandHandler(
     IDoctorProfileRepository doctorProfileRepository,
     IUnitOfWork unitOfWork)
     : IRequestHandler<UpdateDoctorStatusCommand>
    {
        public async Task Handle(
            UpdateDoctorStatusCommand request,
            CancellationToken cancellationToken)
        {
            var doctor = await doctorProfileRepository.GetByIdForUpdateAsync(
                request.DoctorId,
                cancellationToken);

            if (doctor is null)
            {
                throw new NotFoundException("Doctor.NotFound");
            }

            doctor.User.Status = request.Status;

            await unitOfWork.SaveChanges(cancellationToken);
        }
    }
}
