using BFF.API.ServiceClient.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BFF.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsApiClient _analytics;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsApiClient analytics, ILogger<AnalyticsController> logger)
    {
        _analytics = analytics;
        _logger = logger;
    }

    [HttpGet("clicks")]
    public async Task<IActionResult> GetClicks()
    {
        _logger.LogInformation("BFF: GetClicks requested by admin");
        var result = await _analytics.ClicksGETAsync();
        return Ok(new { success = true, message = "", data = result?.Data ?? [] });
    }

    [HttpGet("conversions")]
    public async Task<IActionResult> GetConversions()
    {
        _logger.LogInformation("BFF: GetConversions requested by admin");
        var result = await _analytics.ConversionsGETAsync();
        return Ok(new { success = true, message = "", data = result?.Data ?? [] });
    }
}
