using Shipping.API.Business.Interfaces;
using Shipping.API.Data.Repositories;
using Shipping.API.Models;
using Shared;

namespace Shipping.API.Business.Implementations;

public class ShippingService(IShipmentRepository repo) : IShippingService
{
    private readonly IShipmentRepository _repo = repo;

    public async Task<ShipmentDto> CreateShipmentAsync(CreateShipmentRequest request)
    {
        if (request.OrderId <= 0 || request.CustomerId <= 0)
            throw new ArgumentException("OrderId and CustomerId must be positive.");

        var tracking = $"TRK-{Random.Shared.Next(200000, 999999)}";

        var shipment = new Data.Models.Shipment
        {
            OrderId = request.OrderId,
            TrackingNumber = tracking,
            Status = ShippingStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(shipment);
        return ToDto(shipment);
    }

    public async Task<ShipmentDto?> GetByOrderIdAsync(int orderId)
    {
        var s = await _repo.GetByOrderIdAsync(orderId);
        return s == null ? null : ToDto(s);
    }

    private static ShipmentDto ToDto(Data.Models.Shipment s) => new()
    {
        Id = s.Id,
        OrderId = s.OrderId,
        TrackingNumber = s.TrackingNumber,
        Status = s.Status
    };
}
