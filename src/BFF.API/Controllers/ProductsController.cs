using BFF.API.ServiceClient.Analytics;
using BFF.API.ServiceClient.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BFF.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductApiClient _products;
    private readonly IAnalyticsApiClient _analytics;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductApiClient products, IAnalyticsApiClient analytics, ILogger<ProductsController> logger)
    {
        _products = products;
        _analytics = analytics;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("BFF: GetAll products");
        var result = await _products.ProductsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("BFF: GetById product {ProductId}", id);
        var result = await _products.Products2Async(id);

        // Fire-and-forget click event — failure must not affect response
        _ = _analytics.ClicksPOSTAsync(new LogClickRequest
        {
            UserId = GetUserId(),
            ProductId = id,
            EventType = "view"
        }).ContinueWith(t => { /* swallow */ }, TaskContinuationOptions.OnlyOnFaulted);

        return Ok(result);
    }

    private int GetUserId()
    {
        var name = User.Identity?.Name ?? "";
        return name.GetHashCode() & 0x7FFFFFFF % 1000 + 1;
    }
}
