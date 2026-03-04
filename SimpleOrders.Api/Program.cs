using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using SimpleOrders.Api.Services;
using SimpleOrders.Shared;
using SimpleOrders.Shared.Dtos;
using SimpleOrders.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<RabbitMqSettings>>().Value);
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<KafkaSettings>>().Value);

builder.Services.AddSingleton<KafkaService>();
builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddSingleton<RavenDbService>();
builder.Services.AddSingleton<OrderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapPost("/k/order",
        async (
            [FromServices] KafkaService kafka,
            [FromServices] OrderService orderService,
            CancellationToken cancellation,
            CreateOrUpdateOrderDto dto) =>
        {
            try
            {
                var order = await orderService.Create(dto);

                try
                {
                    await kafka.SendMessage("tp-create-orders", JsonSerializer.Serialize(order), cancellation);
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }

                return Results.Created("/order", order);
            }
            catch (Exception err)
            {
                return Results.BadRequest(err.Message);
            }
        })
    .WithName("PostOrder")
    .WithDescription("Essa requisição irá enviar uma mensagem para um tópico no Apache Kafka.");

app.MapPost("/r/hello", async (RabbitMqService rabbitMq) =>
    {
        try
        {
            await rabbitMq.Publish(new RabbitMessage("general.first.contact", DateTimeOffset.Now.ToString()));
            return Results.Ok();
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }
    })
    .WithName("PostOrderV2")
    .WithDescription("Essa requisição enviará uma mensagem para uma fila do RabbitMQ.");

app.MapPost("/r/order", async (OrderService orderService, RabbitMqService rabbitMq, CreateOrUpdateOrderDto dto) =>
    {
        try
        {
            var order = await orderService.Create(dto);
            rabbitMq.PublishAndForget(new RabbitMessage("create.orders", JsonSerializer.Serialize(order), "orders"));
            return Results.Created("/order", order);
        }
        catch (Exception err)
        {
            return Results.BadRequest(err.Message);
        }
    })
    .WithName("PostOrderRabbit")
    .WithDescription("Essa requisição enviará uma mensagem para uma fila do RabbitMQ.");

app.MapPut("/r/order/{id}",
        async (OrderService orderService, RabbitMqService rabbitMq, string id, CreateOrUpdateOrderDto dto) =>
        {
            try
            {
                var order = await orderService.Update(id, dto);
                if (order == null) return Results.NotFound();

                try
                {
                    await rabbitMq.Publish(new RabbitMessage("update.orders", JsonSerializer.Serialize(order), "orders"));
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                    
                }

                return Results.Ok(order);
            }
            catch (Exception err)
            {
                return Results.BadRequest(err.Message);
            }
        })
    .WithName("PutOrderRabbit")
    .WithDescription("Essa requisição enviará uma mensagem para uma fila do RabbitMQ.");

app.Run();