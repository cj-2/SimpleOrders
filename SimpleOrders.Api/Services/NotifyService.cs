using Confluent.Kafka;

namespace SimpleOrders.Api.Services;

public class NotifyService(KafkaService kafka)
{
    public async Task Handle(string topic, string message)
    {
        await kafka.SendMessage(topic, message);
    }
}