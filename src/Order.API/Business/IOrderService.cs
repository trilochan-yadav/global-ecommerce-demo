using Order.API.Models;

namespace Order.API.Business;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request);
    Task<OrderDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<OrderDto>> GetByCustomerAsync(int customerId);
}
