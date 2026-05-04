using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payment.API.Business.Exceptions;
using Payment.API.Business.Interfaces;
using Payment.API.Models;
using Shared;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService service, ILogger<PaymentsController> logger) : ControllerBase
{
    private readonly IPaymentService _service = service;
    private readonly ILogger<PaymentsController> _logger = logger;

    [HttpPost("process")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Process(ProcessPaymentRequest request)
    {
        _logger.LogInformation("Processing payment for order {OrderId}", request.OrderId);
        try
        {
            var result = await _service.ProcessAsync(request);
            _logger.LogInformation("Payment succeeded for order {OrderId}", request.OrderId);
            return Ok(ApiResponse<PaymentDto>.Ok(result));
        }
        catch (PaymentDeclinedException ex)
        {
            _logger.LogWarning("Payment failed for order {OrderId}", request.OrderId);
            return StatusCode(402, ApiResponse<PaymentDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Circuit breaker open for order {OrderId}: {Message}", request.OrderId, ex.Message);
            return StatusCode(503, ApiResponse<PaymentDto>.Fail(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetById(int id)
    {
        _logger.LogInformation("GetById payment {PaymentId}", id);
        var payment = await _service.GetByIdAsync(id);
        if (payment == null)
        {
            _logger.LogWarning("Payment {PaymentId} not found", id);
            return NotFound(ApiResponse<PaymentDto>.Fail("Payment not found"));
        }
        return Ok(ApiResponse<PaymentDto>.Ok(payment));
    }
}
