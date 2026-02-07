using System.Text.Json;
using Scalar.AspNetCore;
using SimpleOrders.Api.Dtos;
using SimpleOrders.Api.Services;
using SimpleOrders.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<KafkaService>();
builder.Services.AddSingleton<NotifyService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapPost("/order", async (NotifyService notify, CreateOrderDto dto) =>
    {
        try
        {
            var order = new Order(dto.Buyer, dto.Products, dto.BuyerEmail);
            order.Id = Guid.NewGuid().ToString();
            
            var message = JsonSerializer.Serialize(order);
            await notify.Handle("tp-new-orders", message);

            return Results.Created("/order", order);
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }

    })
    .WithName("PostOrder");

app.Run();