using SimpleOrders.Shared.Dtos;
using SimpleOrders.Shared.Entities;
using SimpleOrders.Shared.Services;

namespace SimpleOrders.Api.Services;

public class OrderService(RavenDbService ravenDbService)
{
    public async Task<Order> Create(CreateOrUpdateOrderDto dto)
    {
        using var session = ravenDbService.DocumentStore.OpenAsyncSession();
        var order = new Order(dto.Buyer, dto.Products, dto.BuyerEmail);
        await session.StoreAsync(order);
        await session.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> Update(string id, CreateOrUpdateOrderDto dto)
    {
        using var session = ravenDbService.DocumentStore.OpenAsyncSession();
        var order = await session.LoadAsync<Order>("orders/" + id);
        if (order == null) return null;
        order.UpdateFromDto(dto);
        await session.StoreAsync(order, order.Id);
        await session.SaveChangesAsync();
        return order;
    }
}