namespace Product.API.Data.Repositories;

public interface IProductRepository
{
    Task<List<Models.Product>> GetAllAsync();
    Task<Models.Product?> GetByIdAsync(int id);
    Task UpdateAsync(Models.Product product);
}