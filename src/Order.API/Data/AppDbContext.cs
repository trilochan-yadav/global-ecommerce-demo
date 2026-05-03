using Microsoft.EntityFrameworkCore;
using Shared;

namespace Order.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasData(
            new Order { Id = 1, CustomerId = 1, ProductId = 1, Quantity = 2, TotalAmount = 29.98m, Status = OrderStatus.Shipped, CreatedAt = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new Order { Id = 2, CustomerId = 2, ProductId = 3, Quantity = 1, TotalAmount = 19.99m, Status = OrderStatus.PaymentFailed, CreatedAt = new DateTime(2024, 1, 11, 0, 0, 0, DateTimeKind.Utc) },
            new Order { Id = 3, CustomerId = 1, ProductId = 5, Quantity = 3, TotalAmount = 74.97m, Status = OrderStatus.Pending, CreatedAt = new DateTime(2024, 1, 12, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
