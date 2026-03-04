using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SimpleOrders.Shared.Services;

namespace SimpleOrders.DumbRabbit;

public class OrdersWorker(ILogger<OrdersWorker> logger, RabbitMqService rabbitMqService) : BackgroundService
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
            logger.LogInformation($"### => {DateTimeOffset.Now} - create.orders => {message}");

            // Regras de negócio
            // var order = JsonSerializer.Deserialize<Order>(message);
            // ...

            await rabbitMqService.Channel!.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
        }
        catch
        {
            await rabbitMqService.Channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, stoppingToken);
        }
    }

    private async Task HandleWithUpdateOrders(BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken)
    {
        try
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            logger.LogInformation($"### => {DateTimeOffset.Now} - update.orders => {message}");

            // Regras de negócio
            // var order = JsonSerializer.Deserialize<Order>(message);
            // ...

            await rabbitMqService.Channel!.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
        }
        catch
        {
            await rabbitMqService.Channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, stoppingToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"### => {DateTimeOffset.Now} - Worker em execução.");

        try
        {
            logger.LogInformation($"### => {DateTimeOffset.Now} - Aguardando configuração do RabbitMQ.");
            while (rabbitMqService.Channel == null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            var consumer = new AsyncEventingBasicConsumer(rabbitMqService.Channel!);

            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                await HandleWithEvents(eventArgs, model, cancellationToken);
            };

            await rabbitMqService.Channel!.BasicConsumeAsync("create.orders", false, consumer, cancellationToken);
            await rabbitMqService.Channel!.BasicConsumeAsync("update.orders", false, consumer, cancellationToken);
        }
        catch (Exception err)
        {
            // logger.LogError(err.GetType().ToString());
            logger.LogError($"### => {DateTimeOffset.Now} - {err.Message}");
            throw;
        }
    }
}