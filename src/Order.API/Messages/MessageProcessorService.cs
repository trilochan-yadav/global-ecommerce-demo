using Order.API.Data;
using Order.API.ServiceClient.Analytics;
using Order.API.ServiceClient.Bff;
using Order.API.ServiceClient.Payment;
using Order.API.ServiceClient.Product;
using Order.API.ServiceClient.Shipping;
using Shared;
using System.Text.Json;

namespace Order.API.Messages;

public class MessageProcessorService(
    IServiceScopeFactory scopeFactory,
    IMessageQueue queue,
    ILogger<MessageProcessorService> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IMessageQueue _queue = queue;
    private readonly ILogger<MessageProcessorService> _logger = logger;
    private readonly string _dlqPath = configuration["DlqPath"] ?? "dlq";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = _queue.Dequeue<OrderMessage>();
            if (message is not null)
                await ProcessOrderAsync(message);
            else
                await Task.Delay(200, stoppingToken);
        }
    }

    private async Task ProcessOrderAsync(OrderMessage message)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var repo = sp.GetRequiredService<IOrderRepository>();
        var products = sp.GetRequiredService<IProductApiClient>();
        var payments = sp.GetRequiredService<IPaymentApiClient>();
        var shipping = sp.GetRequiredService<IShippingApiClient>();
        var analytics = sp.GetRequiredService<IAnalyticsApiClient>();
        var bff = sp.GetRequiredService<IBffNotificationApiClient>();

        var order = await repo.GetByIdAsync(message.OrderId);
        if (order is null) return;

        // Step 0: reduce stock — fail → Failed
        var stockOk = await ReduceStockAsync(products, order.ProductId, order.Quantity);
        if (!stockOk)
        {
            order.Status = OrderStatus.Failed;
            await repo.UpdateAsync(order);
            await bff.NotifyAsync(new NotifyRequest { OrderId = order.Id, Status = OrderStatus.Failed.ToString() });
            _logger.LogWarning("Order {OrderId}: stock reduction failed → Failed", order.Id);
            return;
        }

        // Step 1: Pending
        order.Status = OrderStatus.Pending;
        await repo.UpdateAsync(order);
        await bff.NotifyAsync(new NotifyRequest { OrderId = order.Id, Status = OrderStatus.Pending.ToString() });
        await Task.Delay(5000);

        // Step 2: payment
        var paymentOk = await ProcessPaymentAsync(payments, order.Id, order.TotalAmount, message.PaymentToken);

        if (!paymentOk)
        {
            await RestoreStockAsync(products, order.ProductId, order.Quantity);
            order.Status = OrderStatus.PaymentFailed;
            await repo.UpdateAsync(order);
            await WriteDlqAsync(order, message.PaymentToken);
            await bff.NotifyAsync(new NotifyRequest { OrderId = order.Id, Status = OrderStatus.PaymentFailed.ToString() });
            return;
        }

        // Step 3: payment processed → ship → log conversion → notify
        order.Status = OrderStatus.PaymentProcessed;
        await repo.UpdateAsync(order);
        await bff.NotifyAsync(new NotifyRequest { OrderId = order.Id, Status = OrderStatus.PaymentProcessed.ToString() });
        await Task.Delay(5000);

        await shipping.ShipmentsPOSTAsync(new CreateShipmentRequest
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId
        });
        order.Status = OrderStatus.Shipped;
        await repo.UpdateAsync(order);
        await analytics.ConversionsPOSTAsync(new LogConversionRequest
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId
        });
        await Task.Delay(5000);
        await bff.NotifyAsync(new NotifyRequest { OrderId = order.Id, Status = OrderStatus.Shipped.ToString() });
        _logger.LogInformation("Order {OrderId}: Shipped", order.Id);
    }

    private async Task<bool> ReduceStockAsync(IProductApiClient products, int productId, int quantity)
    {
        try
        {
            await products.StockAsync(productId, new UpdateStockRequest
            {
                Quantity = quantity,
                Action = (ServiceClient.Product.StockAction)Shared.StockAction.Reduce
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reduce stock for product {ProductId}", productId);
            return false;
        }
    }

    private async Task RestoreStockAsync(IProductApiClient products, int productId, int quantity)
    {
        try
        {
            await products.StockAsync(productId, new UpdateStockRequest
            {
                Quantity = quantity,
                Action = (ServiceClient.Product.StockAction)Shared.StockAction.Restore
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore stock for product {ProductId}", productId);
        }
    }

    private async Task<bool> ProcessPaymentAsync(
        IPaymentApiClient payments, int orderId, decimal amount, string? paymentToken)
    {
        try
        {
            await payments.ProcessAsync(new ProcessPaymentRequest
            {
                OrderId = orderId,
                Amount = (double)amount,
                PaymentToken = paymentToken
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Payment failed for order {OrderId}", orderId);
            return false;
        }
    }

    private async Task WriteDlqAsync(Order.API.Data.Order order, string? paymentToken)
    {
        Directory.CreateDirectory(_dlqPath);
        var fileName = Path.Combine(_dlqPath, $"{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
        var payload = new
        {
            order.Id,
            order.CustomerId,
            order.ProductId,
            order.Quantity,
            order.TotalAmount,
            PaymentToken = paymentToken,
            FailedAt = DateTime.UtcNow,
            Reason = "Payment failed after 3 attempts"
        };
        await File.WriteAllTextAsync(fileName,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        _logger.LogWarning("DLQ: order {OrderId} written to {File}", order.Id, fileName);
    }
}
