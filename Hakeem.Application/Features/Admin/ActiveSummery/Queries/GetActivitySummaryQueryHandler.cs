using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Admin.ActiveSummery.DTOs;
using Hakeem.Application.Repositories.ActiveSummery;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Hakeem.Application.Features.Admin.ActiveSummery.Queries
{
    public sealed class GetActivitySummaryQueryHandler(
    IActivitySummaryRepository activitySummaryRepository)
    : IRequestHandler<GetActivitySummaryQuery, ActivitySummaryDto>
    {
        public async Task<ActivitySummaryDto> Handle(
            GetActivitySummaryQuery request,
            CancellationToken cancellationToken)
        {
            var fromDate = request.FromDate
                ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var toDate = request.ToDate
                ?? fromDate;

            if (fromDate > toDate)
            {
                throw new LocalizedHttpException(
                ErrorCodes.ValidationInvalidRange,
                400);
            }

            var fromDateTime = fromDate.ToDateTime(TimeOnly.MinValue);

            var toDateTime = toDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            return await activitySummaryRepository.GetActivitySummaryAsync(
                fromDateTime,
                toDateTime,
                cancellationToken);
        }
    }

}
