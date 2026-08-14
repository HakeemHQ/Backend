using Hakeem.Application.Features.Admin.ActiveSummery.DTOs;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Repositories.ActiveSummery
{
    public interface IActivitySummaryRepository : IScoped
    {
        Task<ActivitySummaryDto> GetActivitySummaryAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken);
    }
}
