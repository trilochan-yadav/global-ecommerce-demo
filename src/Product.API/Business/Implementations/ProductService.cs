using Microsoft.Extensions.Logging;
using Product.API.Business.Interfaces;
using Product.API.Data.Repositories;
using Product.API.Models;
using Shared;

namespace Product.API.Business.Implementations;

public class ProductService(IProductRepository repo, ILogger<ProductService> logger) : IProductService
{
    private readonly IProductRepository _repo = repo;
    private readonly ILogger<ProductService> _logger = logger;

    public async Task<List<ProductDto>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all products");
        var products = await _repo.GetAllAsync();
        return products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            StockQuantity = p.StockQuantity
        }).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p == null) return null;
        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            StockQuantity = p.StockQuantity
        };
    }

    public async Task<bool> UpdateStockAsync(int productId, UpdateStockRequest request)
    {
        if (request.Quantity <= 0) return false;

        var product = await _repo.GetByIdAsync(productId);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for stock update", productId);
            return false;
        }

        if (request.Action == StockAction.Reduce)
        {
            if (product.StockQuantity < request.Quantity)
            {
                _logger.LogWarning("Insufficient stock for product {ProductId}: available {Available}, requested {Requested}", productId, product.StockQuantity, request.Quantity);
                return false;
            }
            product.StockQuantity -= request.Quantity;
        }
        else
        {
            product.StockQuantity += request.Quantity;
        }

        await _repo.UpdateAsync(product);
        _logger.LogInformation("Stock updated for product {ProductId}: new quantity {Quantity}", productId, product.StockQuantity);
        return true;
    }
}
