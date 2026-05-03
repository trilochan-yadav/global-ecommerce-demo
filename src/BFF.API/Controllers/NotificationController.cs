using BFF.API.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BFF.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "notification")]
public class NotificationController : ControllerBase
{
    private readonly IHubContext<OrderStatusHub> _hub;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(IHubContext<OrderStatusHub> hub, ILogger<NotificationController> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    [HttpPost("notify")]
    public async Task<IActionResult> Notify([FromBody] NotifyRequest request)
    {
        _logger.LogInformation("Notify order {OrderId} status {Status}", request.OrderId, request.Status);
        await _hub.Clients
            .Group($"order-{request.OrderId}")
            .SendAsync("OrderStatusUpdated", request.OrderId.ToString(), request.Status);

        return Ok(new { success = true });
    }
}

public record NotifyRequest(int OrderId, string Status);
