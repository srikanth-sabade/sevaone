using Microsoft.AspNetCore.Mvc;
using SevaOne.Business.Portal.Interfaces;

namespace SevaOne.Services.Controllers;

[Route("api/v1/notifications")]
public sealed class GovNotificationController : SevaControllerBase
{
    private readonly IGovNotificationService _notifications;

    public GovNotificationController(IGovNotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _notifications.GetPublicAsync(ct));
}
