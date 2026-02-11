using System.Text.Json;
using Confluent.Kafka;
using SimpleOrders.Shared.Entities;

namespace SimpleOrders.Notify;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker iniciado em: {time}", DateTimeOffset.Now);
            }

            var config = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServer"],
                GroupId = "gp-notify",
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Latest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            
            try
            {
                consumer.Subscribe("tp-new-orders");

                while (true)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        var order = JsonSerializer.Deserialize<Order>(result.Message.Value);
                        logger.LogInformation($"Enviando email: {order?.BuyerEmail}");
                        consumer.Commit(result);
                    }
                    catch (Exception err)
                    {
                        logger.LogError($"Message: {err.Message}");
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception err)
            {
                consumer.Close();
                logger.LogError($"Subscribe: {err.Message}");
                Thread.Sleep(10000);
            }
        }
    }
}