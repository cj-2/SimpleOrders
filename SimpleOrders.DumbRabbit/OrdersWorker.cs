using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SimpleOrders.Shared.Services;

namespace SimpleOrders.DumbRabbit;

public class OrdersWorker(ILogger<OrdersWorker> logger, RabbitService rabbitService) : BackgroundService
{
    private async Task HandleWithEvents(BasicDeliverEventArgs eventArgs, object model, CancellationToken stoppingToken)
    {
        if (eventArgs.RoutingKey == "create.orders")
            await HandleWithCreateOrders(eventArgs, stoppingToken);
        else if (eventArgs.RoutingKey == "update.orders")
            await HandleWithUpdateOrders(eventArgs, stoppingToken);
    }

    private async Task HandleWithCreateOrders(BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken)
    {
        try
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            logger.LogInformation($"### create.orders => {message}");

            // Regras de negócio
            // var order = JsonSerializer.Deserialize<Order>(message);
            // ...

            await rabbitService.Channel!.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
        }
        catch
        {
            await rabbitService.Channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, stoppingToken);
        }
    }

    private async Task HandleWithUpdateOrders(BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken)
    {
        try
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            logger.LogInformation($"### update.orders => {message}");

            // Regras de negócio
            // var order = JsonSerializer.Deserialize<Order>(message);
            // ...

            await rabbitService.Channel!.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
        }
        catch
        {
            await rabbitService.Channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, stoppingToken);
        }
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("### => Worker executado em: {time}", DateTimeOffset.Now);

        try
        {
            await rabbitService.Configure(stoppingToken);
            var consumer = new AsyncEventingBasicConsumer(rabbitService.Channel!);

            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                await HandleWithEvents(eventArgs, model, stoppingToken);
            };

            await rabbitService.Channel!.BasicConsumeAsync("create.orders", false, consumer, stoppingToken);
            await rabbitService.Channel!.BasicConsumeAsync("update.orders", false, consumer, stoppingToken);
        }
        catch (Exception err)
        {
            logger.LogError(err.GetType().ToString());
            logger.LogError($"### => Geral: {err.Message}");
            throw;
        }
    }
}