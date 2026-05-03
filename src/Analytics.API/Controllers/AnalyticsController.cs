using Analytics.API.Business.Interfaces;
using Analytics.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared;

namespace Analytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(IAnalyticsService service, ILogger<AnalyticsController> logger) : ControllerBase
{
    private readonly IAnalyticsService _service = service;
    private readonly ILogger<AnalyticsController> _logger = logger;

    [HttpGet("clicks")]
    public async Task<ApiResponse<List<ClickEventDto>>> GetClicks()
    {
        _logger.LogInformation("GetClicks requested");
        return ApiResponse<List<ClickEventDto>>.Ok(await _service.GetClicksAsync());
    }

    [HttpGet("conversions")]
    public async Task<ApiResponse<List<ConversionStatDto>>> GetConversions()
    {
        _logger.LogInformation("GetConversions requested");
        return ApiResponse<List<ConversionStatDto>>.Ok(await _service.GetConversionsAsync());
    }

    [HttpPost("clicks")]
    public async Task<ActionResult<ApiResponse<string>>> LogClick(LogClickRequest request)
    {
        _logger.LogInformation("LogClick for product {ProductId} user {UserId}", request.ProductId, request.UserId);
        await _service.LogClickAsync(request);
        return Ok(ApiResponse<string>.Ok("Click logged"));
    }

    [HttpPost("conversions")]
    public async Task<ActionResult<ApiResponse<string>>> LogConversion(LogConversionRequest request)
    {
        _logger.LogInformation("LogConversion for order {OrderId}", request.OrderId);
        await _service.LogConversionAsync(request);
        return Ok(ApiResponse<string>.Ok("Conversion logged"));
    }
}
