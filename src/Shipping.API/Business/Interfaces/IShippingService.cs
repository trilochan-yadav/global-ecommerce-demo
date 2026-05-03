using Shipping.API.Models;

namespace Shipping.API.Business.Interfaces;

public interface IShippingService
{
    Task<ShipmentDto> CreateShipmentAsync(CreateShipmentRequest request);
    Task<ShipmentDto?> GetByOrderIdAsync(int orderId);
}
