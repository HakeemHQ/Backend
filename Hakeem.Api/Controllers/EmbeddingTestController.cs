using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Authorization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("dev/embeddings")]
[AllowAnonymous]
public sealed class EmbeddingTestController : ApiControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IWebHostEnvironment _environment;

    public EmbeddingTestController(
        IEmbeddingService embeddingService,
        IWebHostEnvironment environment,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _embeddingService = embeddingService;
        _environment = environment;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(GenericResponseModel<TestEmbeddingResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateEmbedding(
        [FromBody] TestEmbeddingRequest request,
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

        var embedding = await _embeddingService.GenerateEmbeddingAsync(
            request.Text.Trim(),
            cancellationToken);

        return SuccessResponse(new TestEmbeddingResponse(
            request.Text.Trim(),
            embedding.Length,
            embedding));
    }
}

public sealed record TestEmbeddingRequest(string Text);

public sealed record TestEmbeddingResponse(
    string Text,
    int Dimensions,
    IReadOnlyList<float> Embedding);
