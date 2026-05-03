using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Product.API.Business.Interfaces;
using Product.API.Models;

namespace Product.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService service, ILogger<ProductsController> logger) : ControllerBase
{
    private readonly IProductService _service = service;
    private readonly ILogger<ProductsController> _logger = logger;

    [HttpGet]
    public async Task<ApiResponse<List<ProductDto>>> GetAll()
    {
        _logger.LogInformation("GetAll products requested");
        return ApiResponse<List<ProductDto>>.Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id)
    {
        _logger.LogInformation("GetById product {ProductId}", id);
        var product = await _service.GetByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found", id);
            return NotFound(ApiResponse<ProductDto>.Fail("Product not found"));
        }
        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    [HttpPatch("{id}/stock")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateStock(int id, UpdateStockRequest request)
    {
        _logger.LogInformation("UpdateStock product {ProductId} action {Action} qty {Quantity}", id, request.Action, request.Quantity);
        var success = await _service.UpdateStockAsync(id, request);
        if (!success)
        {
            _logger.LogWarning("Stock update failed for product {ProductId}", id);
            return BadRequest(ApiResponse<string>.Fail("Stock update failed"));
        }
        return Ok(ApiResponse<string>.Ok("Stock updated"));
    }
}