namespace Order.API.Messages;

public class OrderMessage
{
    public int OrderId { get; set; }
    public string? PaymentToken { get; set; }
}
