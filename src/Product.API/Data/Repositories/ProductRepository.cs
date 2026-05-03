using Microsoft.EntityFrameworkCore;

namespace Product.API.Data.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    private readonly AppDbContext _db = db;

    public Task<List<Models.Product>> GetAllAsync() => _db.Products.ToListAsync();

    public Task<Models.Product?> GetByIdAsync(int id) => _db.Products.FindAsync(id).AsTask();

    public async Task UpdateAsync(Models.Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }
}