using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared;
using Shipping.API.Business.Interfaces;
using Shipping.API.Models;

namespace Shipping.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShipmentsController(IShippingService service, ILogger<ShipmentsController> logger) : ControllerBase
{
    private readonly IShippingService _service = service;
    private readonly ILogger<ShipmentsController> _logger = logger;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> Create(CreateShipmentRequest request)
    {
        _logger.LogInformation("Creating shipment for order {OrderId}", request.OrderId);
        try
        {
            var result = await _service.CreateShipmentAsync(request);
            _logger.LogInformation("Shipment created for order {OrderId}", request.OrderId);
            return Ok(ApiResponse<ShipmentDto>.Ok(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid shipment request for order {OrderId}: {Message}", request.OrderId, ex.Message);
            return BadRequest(ApiResponse<ShipmentDto>.Fail(ex.Message));
        }
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<ApiResponse<ShipmentDto>>> GetByOrderId(int orderId)
    {
        _logger.LogInformation("GetByOrderId shipment for order {OrderId}", orderId);
        var shipment = await _service.GetByOrderIdAsync(orderId);
        if (shipment == null)
        {
            _logger.LogWarning("Shipment not found for order {OrderId}", orderId);
            return NotFound(ApiResponse<ShipmentDto>.Fail("Shipment not found"));
        }
        return Ok(ApiResponse<ShipmentDto>.Ok(shipment));
    }
}
