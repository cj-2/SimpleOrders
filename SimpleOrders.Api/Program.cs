using System.Text.Json;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using SimpleOrders.Api.Dtos;
using SimpleOrders.Shared;
using SimpleOrders.Shared.Entities;
using SimpleOrders.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<RabbitMqSettings>>().Value);
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<KafkaSettings>>().Value);

builder.Services.AddSingleton<KafkaService>();
builder.Services.AddSingleton<RabbitService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapPost("/k/order", async (KafkaService kafka, CreateOrderDto dto, IConfiguration c) =>
    {
        try
        {
            var order = new Order(Guid.NewGuid().ToString(), dto.Buyer, dto.Products, dto.BuyerEmail);
            var message = JsonSerializer.Serialize(order);
            await kafka.SendMessage("tp-create-orders", message);
            return Results.Created("/order", order);
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }
    })
    .WithName("PostOrder")
    .WithDescription("Essa requisição irá enviar uma mensagem para um tópico no Apache Kafka.");

app.MapPost("/r/hello", async (RabbitService rabbit) =>
    {
        try
        {
            await rabbit.Publish("general.first.contact", DateTimeOffset.Now.ToString());
            return Results.Ok();
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }
    })
    .WithName("PostOrderV2")
    .WithDescription("Essa requisição enviará uma mensagem para uma fila do RabbitMQ.");

app.MapPost("/r/order", async (RabbitService rabbit, CreateOrderDto dto) =>
    {
        try
        {
            var order = new Order(Guid.NewGuid().ToString(), dto.Buyer, dto.Products, dto.BuyerEmail);
            var message = JsonSerializer.Serialize(order);
            await rabbit.Publish("create.orders", message, exchange: "orders");
            return Results.Created("/order", order);
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }
    })
    .WithName("PostOrderRabbit")
    .WithDescription("Essa requisição enviará uma mensagem para uma fila do RabbitMQ.");

app.MapPut("/r/order/{id}", async (RabbitService rabbit, string id, CreateOrderDto dto) =>
    {
        try
        {
            var order = new Order(id, dto.Buyer, dto.Products, dto.BuyerEmail);
            var message = JsonSerializer.Serialize(order);
            await rabbit.Publish("update.orders", message, exchange: "orders");
            return Results.Ok();
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }
    })
    .WithName("PutOrderRabbit")
    .WithDescription("Essa requisição enviará uma mensagem para uma fila do RabbitMQ.");

app.Run();