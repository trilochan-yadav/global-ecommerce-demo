namespace Payment.API.Models;

public class ProcessPaymentRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentToken { get; set; } = string.Empty;
}
