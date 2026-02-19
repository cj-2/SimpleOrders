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
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            try
            {
                consumer.Subscribe("tp-create-orders");

                while (true)
                {
                    try
                    {
                        logger.LogInformation("Iniciando consumo: tp-create-orders");
                        var result = consumer.Consume(stoppingToken);
                        var order = JsonSerializer.Deserialize<Order>(result.Message.Value);

                        logger.LogInformation($"To: {order?.BuyerEmail} " +
                                              $"Mensagem: " +
                                              $"Olá {order.Buyer}, acabamos de receber seu pedido, " +
                                              $"e já estamos preparando seu {order.Products[0]} e outros itens do pedido!");

                        consumer.Commit(result);
                    }
                    catch (ConsumeException err)
                    {
                        if (err.Error.Code == ErrorCode.UnknownTopicOrPart)
                            throw;

                        logger.LogError($"ConsumeException: {err.Message}");
                        logger.LogInformation("O processo será reiniciado em 5 segundos.");
                        Thread.Sleep(5000);
                    }
                    catch (Exception err)
                    {
                        logger.LogError($"Message: {err.Message}");
                        logger.LogInformation("O processo será reiniciado em 5 segundos.");
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception err)
            {
                consumer.Close();
                logger.LogError($"Subscribe: {err.Message}");
                logger.LogInformation("O processo será reiniciado em 10 segundos.");
                Thread.Sleep(10000);
            }
        }
    }
}