namespace Shipping.API.Data.Models;

public class Shipment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public Shared.ShippingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
