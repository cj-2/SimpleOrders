using System.Text.Json;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using SimpleOrders.Api.Dtos;
using SimpleOrders.Api.Services;
using SimpleOrders.Shared;
using SimpleOrders.Shared.Entities;
using SimpleOrders.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<RabbitMqConfig>>().Value);

builder.Services.AddSingleton<KafkaService>();
builder.Services.AddSingleton<RabbitService>();
builder.Services.AddSingleton<NotifyService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

Console.WriteLine(app.Configuration["RabbitMQ:HostName"]);


app.MapPost("/order", async (NotifyService notify, CreateOrderDto dto, IConfiguration c) =>
    {
        try
        {
            var order = new Order(Guid.NewGuid().ToString(), dto.Buyer, dto.Products, dto.BuyerEmail);
            var message = JsonSerializer.Serialize(order);
            await notify.Handle("tp-create-orders", message);
            return Results.Created("/order", order);
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }

    })
    .WithName("PostOrder");

app.MapPost("/order-v2", async (RabbitService rabbit, CreateOrderDto dto) =>
    {
        try
        {
            var order = new Order(Guid.NewGuid().ToString(), dto.Buyer, dto.Products, dto.BuyerEmail);
            var message = JsonSerializer.Serialize(order);
            await rabbit.Publish("create.orders", message);
            return Results.Created("/order", order);
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }

    })
    .WithName("PostOrderV2");

app.Run();