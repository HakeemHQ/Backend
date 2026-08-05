using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecords
{
    public sealed record GetMedicalRecordsQuery(string? Search,string? RecordType,DateTime? FromDate,
                                                 DateTime? ToDate,int PageNumber = 1, int PageSize = 20) 
        : IRequest<PaginatedResult<MedicalRecordDto>>;
}
