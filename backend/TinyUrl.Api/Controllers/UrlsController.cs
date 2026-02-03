using Microsoft.AspNetCore.Mvc;
using TinyUrl.Api.Domain;
using TinyUrl.Api.Infrastructure.Services;
using TinyUrl.Api.Models;

namespace TinyUrl.Api.Controllers;

[ApiController]
public sealed class UrlsController : ControllerBase
{
    private readonly IUrlService _service;
    private readonly IUserContext _userContext;

    public UrlsController(IUrlService service, IUserContext userContext)
    {
        _service = service;
        _userContext = userContext;
    }

    [HttpPost("api/urls")]
    public async Task<ActionResult<CreateUrlResponse>> Create([FromBody] CreateUrlRequest request)
    {
        try
        {
            var ownerId = _userContext.GetOrThrow(HttpContext);
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _service.CreateAsync(ownerId, request, baseUrl);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("api/urls/{shortCode}")]
    public async Task<IActionResult> Delete([FromRoute] string shortCode)
    {
        var ownerId = _userContext.GetOrThrow(HttpContext);
        var deleted = await _service.DeleteAsync(ownerId, shortCode);
        return deleted ? NoContent() : NotFound();
    }

    // Returns the long URL and increments click count.
    [HttpGet("api/urls/{shortCode}")]
    public async Task<ActionResult<ResolveUrlResponse>> Resolve([FromRoute] string shortCode)
    {
        var ownerId = _userContext.GetOrThrow(HttpContext);
        var resolved = await _service.ResolveAsync(ownerId, shortCode);
        return resolved is null ? NotFound() : Ok(resolved);
    }

    [HttpGet("api/urls/{shortCode}/stats")]
    public async Task<ActionResult<UrlStatsResponse>> Stats([FromRoute] string shortCode)
    {
        var ownerId = _userContext.GetOrThrow(HttpContext);
        var stats = await _service.GetStatsAsync(ownerId, shortCode);
        return stats is null ? NotFound() : Ok(stats);
    }

    [HttpGet("api/urls")]
    public async Task<ActionResult<IReadOnlyList<UrlListItemResponse>>> List()
    {
        var ownerId = _userContext.GetOrThrow(HttpContext);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var list = await _service.ListAsync(ownerId, baseUrl);
        return Ok(list);
    }

    // Convenience redirect endpoint for actual browser navigation.
    [HttpGet("r/{shortCode}")]
    public async Task<IActionResult> RedirectToLong([FromRoute] string shortCode)
    {
        var ownerId = _userContext.GetOrThrow(HttpContext);
        var resolved = await _service.ResolveAsync(ownerId, shortCode);
        return resolved is null ? NotFound() : Redirect(resolved.LongUrl);
    }
}
