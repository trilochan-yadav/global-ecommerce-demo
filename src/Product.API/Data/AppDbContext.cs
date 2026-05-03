using Microsoft.EntityFrameworkCore;

namespace Product.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Models.Product> Products => Set<Models.Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Product>().HasData(

            new Models.Product { Id = 1, Name = "Laptop Pro 15", Price = 1299.99m, StockQuantity = 50 },
            new Models.Product { Id = 2, Name = "Wireless Mouse", Price = 29.99m, StockQuantity = 200 },
            new Models.Product { Id = 3, Name = "USB-C Hub", Price = 49.99m, StockQuantity = 150 },
            new Models.Product { Id = 4, Name = "4K Monitor", Price = 599.99m, StockQuantity = 30 },
            new Models.Product { Id = 5, Name = "Mechanical Keyboard", Price = 89.99m, StockQuantity = 80 },
            new Models.Product { Id = 6, Name = "Webcam HD", Price = 69.99m, StockQuantity = 120 },
            new Models.Product { Id = 7, Name = "Noise Cancelling Headphones", Price = 249.99m, StockQuantity = 60 },
            new Models.Product { Id = 8, Name = "Phone Stand", Price = 19.99m, StockQuantity = 300 }
        );
    }
}