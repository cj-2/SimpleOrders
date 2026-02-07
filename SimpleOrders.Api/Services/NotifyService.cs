using Confluent.Kafka;

namespace SimpleOrders.Api.Services;

public class NotifyService(KafkaService kafka)
{
    public async Task Handle(string topic, string message)
    {
        await kafka.NotifyProducer.ProduceAsync(topic, new Message<Null, string> { Value = message });
    }
}