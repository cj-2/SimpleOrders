using System.Text.Json.Serialization;

namespace SimpleOrders.Shared.Entities;

public class Order
{
    public string? Id { get; set; } = null;
    public string Buyer { get; set; }
    public string BuyerEmail { get; set; }
    public string[] Products { get; set; }

    public Order()
    {
    }

    public Order(string buyer, string[] products, string buyerEmail)
    {
        Validade(buyer, products, buyerEmail);

        Buyer = buyer;
        Products = products;
        BuyerEmail = buyerEmail;
    }

    [JsonConstructor]
    public Order(string id, string buyer, string[] products, string buyerEmail)
    {
        if (string.IsNullOrEmpty(id))
            throw new Exception("id não pode ser nulo.");

        Validade(buyer, products, buyerEmail);

        Id = id;
        Buyer = buyer;
        Products = products;
        BuyerEmail = buyerEmail;
    }

    private static void Validade(string? buyer, string[]? products, string? buyerEmail)
    {
        if (string.IsNullOrEmpty(buyer))
            throw new Exception("buyer é obrigatório.");
        
        if (string.IsNullOrEmpty(buyerEmail))
            throw new Exception("buyerEmail é obrigatório.");

        if (products == null)
            throw new Exception("products é obrigatório.");

        if (products == null || products.Length == 0)
            throw new Exception("products não pode ser vazio.");
    }
}