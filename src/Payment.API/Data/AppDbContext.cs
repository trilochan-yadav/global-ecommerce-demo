using Microsoft.EntityFrameworkCore;
using Shared;

namespace Payment.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Models.Payment> Payments => Set<Models.Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Payment>().HasData(
            new Models.Payment { Id = 1, OrderId = 1, Amount = 1299.99m, Status = PaymentStatus.Completed, CreatedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc) },
            new Models.Payment { Id = 2, OrderId = 2, Amount = 29.99m, Status = PaymentStatus.Completed, CreatedAt = new DateTime(2026, 4, 2, 11, 0, 0, DateTimeKind.Utc) },
            new Models.Payment { Id = 3, OrderId = 3, Amount = 49.99m, Status = PaymentStatus.Failed, CreatedAt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc) }
        );
    }
}
