using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("dev/qdrant")]
[AllowAnonymous]
public sealed class QdrantTestController : ApiControllerBase
{
    private readonly IQdrantTestService _qdrantTestService;
    private readonly IWebHostEnvironment _environment;

    public QdrantTestController(
        IQdrantTestService qdrantTestService,
        IWebHostEnvironment environment,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _qdrantTestService = qdrantTestService;
        _environment = environment;
    }

    [HttpPost("upsert")]
    [ProducesResponseType(
        typeof(GenericResponseModel<TestQdrantUpsertResult>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertTestDocument(
        [FromBody] TestQdrantTextRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.InvalidRequest"].Value,
                "Validation.InvalidRequest"));
        }

        var result = await _qdrantTestService.UpsertTestDocumentAsync(
            request.Text.Trim(),
            cancellationToken);

        return SuccessResponse(result);
    }

    [HttpPost("search")]
    [ProducesResponseType(
        typeof(GenericResponseModel<IReadOnlyList<TestQdrantSearchResult>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromBody] TestQdrantSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.InvalidRequest"].Value,
                "Validation.InvalidRequest"));
        }

        var limit = request.Limit <= 0 ? 5 : request.Limit;
        var results = await _qdrantTestService.SearchAsync(
            request.Query.Trim(),
            limit,
            cancellationToken);

        return SuccessResponse(results);
    }
}

public sealed record TestQdrantTextRequest(string Text);

public sealed record TestQdrantSearchRequest(string Query, int Limit = 5);
