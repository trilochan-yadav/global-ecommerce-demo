using Microsoft.EntityFrameworkCore;
using Shared;

namespace Shipping.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Models.Shipment> Shipments => Set<Models.Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Shipment>().HasData(
            new Models.Shipment { Id = 1, OrderId = 1, TrackingNumber = "TRK-100001", Status = ShippingStatus.Delivered, CreatedAt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc) },
            new Models.Shipment { Id = 2, OrderId = 2, TrackingNumber = "TRK-100002", Status = ShippingStatus.InTransit, CreatedAt = new DateTime(2026, 4, 2, 13, 0, 0, DateTimeKind.Utc) },
            new Models.Shipment { Id = 3, OrderId = 3, TrackingNumber = "TRK-100003", Status = ShippingStatus.Created, CreatedAt = new DateTime(2026, 4, 3, 14, 0, 0, DateTimeKind.Utc) }
        );
    }
}
