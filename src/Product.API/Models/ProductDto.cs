namespace Product.API.Models;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}

public class UpdateStockRequest
{
    public int Quantity { get; set; }
    public Shared.StockAction Action { get; set; }
}