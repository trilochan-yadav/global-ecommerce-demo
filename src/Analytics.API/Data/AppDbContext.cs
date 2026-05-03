using Microsoft.EntityFrameworkCore;

namespace Analytics.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Models.ClickEvent> ClickEvents => Set<Models.ClickEvent>();
    public DbSet<Models.ConversionStat> ConversionStats => Set<Models.ConversionStat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.ClickEvent>().HasData(
            new Models.ClickEvent { Id = 1, UserId = 1, ProductId = 1, EventType = "view", CreatedAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 2, UserId = 1, ProductId = 2, EventType = "view", CreatedAt = new DateTime(2026, 4, 1, 9, 10, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 3, UserId = 2, ProductId = 1, EventType = "view", CreatedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 4, UserId = 2, ProductId = 3, EventType = "view", CreatedAt = new DateTime(2026, 4, 1, 10, 15, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 5, UserId = 3, ProductId = 4, EventType = "view", CreatedAt = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 6, UserId = 1, ProductId = 1, EventType = "add_cart", CreatedAt = new DateTime(2026, 4, 2, 8, 30, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 7, UserId = 2, ProductId = 3, EventType = "add_cart", CreatedAt = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 8, UserId = 3, ProductId = 5, EventType = "view", CreatedAt = new DateTime(2026, 4, 3, 11, 0, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 9, UserId = 4, ProductId = 2, EventType = "add_cart", CreatedAt = new DateTime(2026, 4, 3, 11, 20, 0, DateTimeKind.Utc) },
            new Models.ClickEvent { Id = 10, UserId = 4, ProductId = 6, EventType = "view", CreatedAt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Models.ConversionStat>().HasData(
            new Models.ConversionStat { Id = 1, OrderId = 1, CustomerId = 1, CreatedAt = new DateTime(2026, 4, 1, 10, 30, 0, DateTimeKind.Utc) },
            new Models.ConversionStat { Id = 2, OrderId = 2, CustomerId = 2, CreatedAt = new DateTime(2026, 4, 2, 9, 45, 0, DateTimeKind.Utc) },
            new Models.ConversionStat { Id = 3, OrderId = 3, CustomerId = 3, CreatedAt = new DateTime(2026, 4, 3, 13, 0, 0, DateTimeKind.Utc) }
        );
    }
}
