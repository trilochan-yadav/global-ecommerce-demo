using Microsoft.AspNetCore.Mvc;
using Order.API.Business;
using Order.API.Models;

namespace Order.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService service, ILogger<OrdersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderDto>>>> GetByCustomer([FromQuery] int customerId)
    {
        _logger.LogInformation("GetByCustomer orders for customer {CustomerId}", customerId);
        var dtos = await _service.GetByCustomerAsync(customerId);
        return Ok(ApiResponse<IReadOnlyList<OrderDto>>.Ok(dtos));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Creating order for customer {CustomerId} product {ProductId}", request.CustomerId, request.ProductId);
        var dto = await _service.CreateAsync(request);
        _logger.LogInformation("Order {OrderId} created and queued for processing", dto.Id);
        return Ok(ApiResponse<OrderDto>.Ok(dto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("GetById order {OrderId}", id);
        var dto = await _service.GetByIdAsync(id);
        if (dto is null)
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            return NotFound(ApiResponse<OrderDto>.Fail("Order not found"));
        }
        return Ok(ApiResponse<OrderDto>.Ok(dto));
    }
}
