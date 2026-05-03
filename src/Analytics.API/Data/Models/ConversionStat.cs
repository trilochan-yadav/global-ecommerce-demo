namespace Analytics.API.Data.Models;

public class ConversionStat
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
}
