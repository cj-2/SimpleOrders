using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SimpleOrders.Shared;
using SimpleOrders.Shared.Entities;
using SimpleOrders.Shared.Services;

namespace SimpleOrders.DumbRabbit;

public class Worker(ILogger<Worker> logger, RabbitService rabbitService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("### => Worker executado em: {time}", DateTimeOffset.Now);

        try
        {
            await rabbitService.Configure();
            var consumer = new AsyncEventingBasicConsumer(rabbitService.Channel!);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                logger.LogInformation($"### => {message}");
                
                // Regras de negócio
                // var order = JsonSerializer.Deserialize<Order>(message);
                // ...
                
                await rabbitService.Channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            };

            await rabbitService.Channel!.BasicConsumeAsync("create.orders", false, consumer, stoppingToken);
        }
        catch (Exception err)
        {
            logger.LogError(err.GetType().ToString());
            logger.LogError($"### => Geral: {err.Message}");
        }
    }
}