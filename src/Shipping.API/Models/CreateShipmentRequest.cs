namespace Shipping.API.Models;

public class CreateShipmentRequest
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
}
