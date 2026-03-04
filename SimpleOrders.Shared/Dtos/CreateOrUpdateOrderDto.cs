namespace SimpleOrders.Shared.Dtos;

public record CreateOrUpdateOrderDto
{
    public string Buyer { get; set; }
    public string BuyerEmail { get; set; }
    public string[] Products { get; set; }
}