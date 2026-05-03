using Product.API.Models;

namespace Product.API.Business.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(int id);
    Task<bool> UpdateStockAsync(int productId, UpdateStockRequest request);
}