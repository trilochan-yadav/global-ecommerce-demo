using Order.API.Data;
using Order.API.Messages;
using Order.API.Models;
using Shared;

namespace Order.API.Business;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;
    private readonly IMessageQueue _queue;

    public OrderService(IOrderRepository repo, IMessageQueue queue)
    {
        _repo = repo;
        _queue = queue;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        var order = new Data.Order
        {
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            TotalAmount = request.UnitPrice * request.Quantity,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(order);
        _queue.Enqueue(new OrderMessage { OrderId = order.Id, PaymentToken = request.PaymentToken });

        return MapToDto(order);
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var order = await _repo.GetByIdAsync(id);
        return order is null ? null : MapToDto(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetByCustomerAsync(int customerId)
    {
        var orders = await _repo.GetByCustomerAsync(customerId);
        return orders.Select(MapToDto).ToList();
    }

    private static OrderDto MapToDto(Data.Order o) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        ProductId = o.ProductId,
        Quantity = o.Quantity,
        TotalAmount = o.TotalAmount,
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt
    };
}
