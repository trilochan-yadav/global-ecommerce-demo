using BFF.API.Business.Interfaces;
using BFF.API.ServiceClient.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BFF.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderApiClient _orders;
    private readonly ICryptoService _crypto;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderApiClient orders, ICryptoService crypto, ILogger<OrdersController> logger)
    {
        _orders = orders;
        _crypto = crypto;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders()
    {
        var username = User.FindFirstValue(ClaimTypes.Name) ?? "guest";
        var customerId = ResolveCustomerId(username);
        _logger.LogInformation("BFF: GetMyOrders for customer {CustomerId} (user: {Username})", customerId, username);
        var result = await _orders.OrdersGETAsync(customerId);
        return Ok(new { success = true, message = "", data = result?.Data ?? [] });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("BFF: Creating order for product {ProductId}", request.ProductId);

        // Decrypt the payment token that was AES-GCM encrypted by the Angular client.
        // Plain tokens never travel over the external network.
        // Derive customer ID from the authenticated user's name claim
        var username = User.FindFirstValue(ClaimTypes.Name) ?? "guest";
        request.CustomerId = ResolveCustomerId(username);

        if (!string.IsNullOrEmpty(request.PaymentToken))
        {
            try
            {
                request.PaymentToken = _crypto.Decrypt(request.PaymentToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BFF: Failed to decrypt payment token for product {ProductId}", request.ProductId);
                return BadRequest(new { success = false, message = "Invalid payment token." });
            }
        }

        await _orders.OrdersPOSTAsync(request);
        return StatusCode(StatusCodes.Status202Accepted, new { success = true, message = "Order accepted" });
    }

    /// <summary>Maps a username to a stable integer customer ID for demo purposes.
    /// Uses FNV-1a so the result is deterministic across process restarts
    /// (string.GetHashCode() is randomised per-process in .NET Core).</summary>
    private static int ResolveCustomerId(string username)
    {
        var lower = username.ToLowerInvariant();
        if (lower == "admin") return 1;

        // FNV-1a 32-bit — deterministic, no external dependencies
        uint hash = 2166136261u;
        foreach (char c in lower)
        {
            hash ^= (byte)c;
            hash *= 16777619u;
        }
        return (int)(hash % 9000u) + 1000;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("BFF: GetById order {OrderId}", id);
        await _orders.OrdersGETAsync(id);
        return Ok(new { success = true });
    }
}
