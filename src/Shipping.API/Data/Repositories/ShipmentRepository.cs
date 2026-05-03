using Microsoft.EntityFrameworkCore;

namespace Shipping.API.Data.Repositories;

public class ShipmentRepository(AppDbContext db) : IShipmentRepository
{
    private readonly AppDbContext _db = db;

    public Task<Models.Shipment?> GetByOrderIdAsync(int orderId) =>
        _db.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId);

    public async Task AddAsync(Models.Shipment shipment)
    {
        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();
    }
}
