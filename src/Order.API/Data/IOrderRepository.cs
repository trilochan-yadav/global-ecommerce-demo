namespace Order.API.Data;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId);
    Task<Order> AddAsync(Order order);
    Task UpdateAsync(Order order);
}
