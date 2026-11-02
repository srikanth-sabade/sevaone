using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.Portal.Interfaces;

namespace SevaOne.Services.Controllers;

[Route("api/v1/service-notice")]
public sealed class ServiceNoticeController : SevaControllerBase
{
    private readonly IServiceNoticeService _serviceNotice;

    public ServiceNoticeController(IServiceNoticeService serviceNotice)
    {
        _serviceNotice = serviceNotice;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => new JsonResult(await _serviceNotice.GetPublicAsync(ct))
    {
        StatusCode = StatusCodes.Status200OK
    };
}
