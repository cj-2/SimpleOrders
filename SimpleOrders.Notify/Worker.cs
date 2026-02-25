using System.Text.Json;
using Confluent.Kafka;
using SimpleOrders.Shared;
using SimpleOrders.Shared.Entities;
using SimpleOrders.Shared.Services;

namespace SimpleOrders.Notify;

public class Worker(ILogger<Worker> logger, KafkaSettings kafkaSettings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("### => Worker executado em: {time}", DateTimeOffset.Now);

        try
        {
            using var kafka = new KafkaService(kafkaSettings);
            await kafka.Configure();
            using var consumer = kafka.CreateConsume<Ignore, string>("gp-notify", "Earliest");
            consumer.Subscribe("tp-create-orders");

            try
            {
                logger.LogInformation("### => Iniciando consumo: tp-create-orders em: {tine}", DateTimeOffset.Now);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        logger.LogInformation($"### => {result.Message.Value}");

                        // O que deve ser feito...
                        // var order = JsonSerializer.Deserialize<Order>(result.Message.Value);

                        consumer.Commit(result);
                    }
                    catch (ConsumeException err)
                    {
                        if (err.Error.Code == ErrorCode.UnknownTopicOrPart)
                            throw;

                        logger.LogError($"### => Consumo: {err.Message}");
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
        }
        catch (Exception err)
        {
            logger.LogError($"### => Geral: {err.Message}");
            throw;
        }
    }
}