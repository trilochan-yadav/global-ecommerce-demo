namespace Payment.API.Data.Models;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public Shared.PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
