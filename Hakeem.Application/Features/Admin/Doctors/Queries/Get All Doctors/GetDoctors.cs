using Hakeem.Application.Features.Admin.Doctors.DTOs;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.Queries
{
    public sealed record GetDoctorsQuery(
        string? Search = null, string? Specialty = null,
        AccountStatus? Status = null, int Page = 1,
        int PageSize = 10)
        
        
        : IRequest<AdminDoctorsResponse>;
}
