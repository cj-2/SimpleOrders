using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SimpleOrders.Shared.Services;

namespace SimpleOrders.DumbRabbit;

public class HelloWorker(ILogger<HelloWorker> logger, RabbitMqService rabbitMqService) : BackgroundService
{
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
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                logger.LogInformation($"### hello => {message}");
                await rabbitMqService.Channel!.BasicAckAsync(eventArgs.DeliveryTag, false, cancellationToken);
            };

            await rabbitMqService.Channel!.BasicConsumeAsync("general.first.contact", false, consumer, cancellationToken);
        }
        catch (Exception err)
        {
            // logger.LogError(err.GetType().ToString());
            logger.LogError($"### => {DateTimeOffset.Now} - Geral: {err.Message}");
            throw;
        }
    }
}