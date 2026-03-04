using Microsoft.AspNetCore.Mvc;
using Santander.HackerNewsApi.Models;
using Santander.HackerNewsApi.Services;

namespace Santander.HackerNewsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BestStoriesController : ControllerBase
{
    private readonly IHackerNewsService _service;

    public BestStoriesController(IHackerNewsService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BestStoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<BestStoryDto>>> Get([FromQuery] int n = 10, CancellationToken ct = default)
    {
        if (n <= 0)
            return BadRequest(new { message = "Query parameter 'n' must be greater than 0." });

        var result = await _service.GetBestStoriesAsync(n, ct);
        return Ok(result);
    }
}
