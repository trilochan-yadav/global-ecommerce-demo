namespace Shipping.API.Data.Repositories;

public interface IShipmentRepository
{
    Task<Models.Shipment?> GetByOrderIdAsync(int orderId);
    Task AddAsync(Models.Shipment shipment);
}
