using Hakeem.Application.Features.Admin.ActiveSummery.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.ActiveSummery.Queries
{
    public sealed record GetActivitySummaryQuery(
    DateOnly? FromDate,
    DateOnly? ToDate)
    : IRequest<ActivitySummaryDto>;
}
