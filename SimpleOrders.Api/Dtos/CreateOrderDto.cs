namespace SimpleOrders.Api.Dtos;

public record CreateOrderDto
{
    public string Buyer { get; set; }
    public string BuyerEmail { get; set; }
    public string[] Products { get; set; }
}