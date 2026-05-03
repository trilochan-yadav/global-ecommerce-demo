using Shared;

namespace Shipping.API.Models;

public class ShipmentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public ShippingStatus Status { get; set; }
}
