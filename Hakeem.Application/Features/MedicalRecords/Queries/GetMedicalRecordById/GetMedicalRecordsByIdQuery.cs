using Hakeem.Application.Features.MedicalRecords.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecordById
{
    public sealed record GetMedicalRecordByIdQuery(Guid MedicalRecordId):IRequest<GetMedicalRecordByIdResult>;
}
