using System.Text.Json;
using Confluent.Kafka;
using SimpleOrders.Shared.Entities;
using SimpleOrders.Shared.Services;

namespace SimpleOrders.Notify;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("### => Worker executado em: {time}", DateTimeOffset.Now);

        try
        {
            using var kafka = new KafkaService(configuration);
            await kafka.Configure();
            using var consumer = kafka.CreateConsume<Ignore, string>("gp-notify", "Earliest");
            consumer.Subscribe("tp-create-orders");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        logger.LogInformation("### => Iniciando consumo: tp-create-orders em: {tine}",
                            DateTimeOffset.Now);

                        var result = consumer.Consume(stoppingToken);
                        var order = JsonSerializer.Deserialize<Order>(result.Message.Value);

                        logger.LogInformation($"### => To: {order?.BuyerEmail} " +
                                              $"Mensagem: " +
                                              $"Olá {order.Buyer}, acabamos de receber seu pedido, " +
                                              $"e já estamos preparando seu {order.Products[0]} " +
                                              $"e outros itens do pedido!");

                        consumer.Commit(result);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (ConsumeException err)
                    {
                        if (err.Error.Code == ErrorCode.UnknownTopicOrPart)
                            throw;

                        logger.LogError($"### => Consumo: {err.Message}");
                        logger.LogInformation("### => O processo será reiniciado em 5 segundos.");
                        Thread.Sleep(5000);
                    }
                    catch (Exception err)
                    {
                        logger.LogError($"### => Consumo (Geral): {err.Message}");
                        logger.LogInformation("### => O processo será reiniciado em 5 segundos.");
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
                throw;
            }
            catch (Exception err)
            {
                consumer.Close();
                logger.LogError($"### => Inscrição: {err.Message}");
                logger.LogInformation("### => O processo será reiniciado em 10 segundos.");
                Thread.Sleep(10000);
            }
        }
        catch (Exception err)
        {
            logger.LogError($"### => Geral: {err.Message}");
            throw;
        }
    }
}