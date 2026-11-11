using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.Portal.Interfaces;

namespace SevaOne.Services.Controllers;

[Route("api/v1/centres")]
public sealed class CentresController : SevaControllerBase
{
    private readonly IGramaOneCentreService _centres;

    public CentresController(IGramaOneCentreService centres)
    {
        _centres = centres;
    }

    /// <summary>Public: the Grama One centres nearest to a point, closest first.</summary>
    [HttpGet("gramaone/nearest")]
    public async Task<IActionResult> Nearest([FromQuery] double lat, [FromQuery] double lon, [FromQuery] int limit = 5, CancellationToken ct = default) =>
        Ok(await _centres.NearestAsync(lat, lon, limit, ct));

    [HttpGet("gramaone/districts")]
    public async Task<IActionResult> Districts(CancellationToken ct) => Ok(await _centres.DistrictsAsync(ct));

    /// <summary>Public: the centres in a district, optionally narrowed by a village, gram panchayat or taluk name.</summary>
    [HttpGet("gramaone")]
    public async Task<IActionResult> Search([FromQuery] string district, [FromQuery] string? q, CancellationToken ct) =>
        Ok(await _centres.SearchAsync(district, q, ct));
}
