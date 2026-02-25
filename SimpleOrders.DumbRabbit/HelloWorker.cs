using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SimpleOrders.Shared.Services;

namespace SimpleOrders.DumbRabbit;

public class HelloWorker(ILogger<HelloWorker> logger, RabbitService rabbitService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("### => Worker executado em: {time}", DateTimeOffset.Now);

        try
        {
            await rabbitService.Configure(stoppingToken);
            var consumer = new AsyncEventingBasicConsumer(rabbitService.Channel!);

            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                logger.LogInformation($"### hello => {message}");
                await rabbitService.Channel!.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
            };

            await rabbitService.Channel!.BasicConsumeAsync("general.first.contact", false, consumer, stoppingToken);
        }
        catch (Exception err)
        {
            logger.LogError(err.GetType().ToString());
            logger.LogError($"### => Geral: {err.Message}");
            throw;
        }
    }
}