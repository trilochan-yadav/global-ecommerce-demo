namespace Analytics.API.Models;

public class ClickEventDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ConversionStatDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LogClickRequest
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public string EventType { get; set; } = string.Empty;
}

public class LogConversionRequest
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
}
