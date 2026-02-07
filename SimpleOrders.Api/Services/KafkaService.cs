using Confluent.Kafka;

namespace SimpleOrders.Api.Services;

public class KafkaService : IDisposable
{
    public IProducer<Null, string> NotifyProducer { get; }

    public KafkaService(IConfiguration configuration)
    {
        var config = new ProducerConfig()
        {
            BootstrapServers = configuration["Kafka:BootstrapServer"]
        };

        NotifyProducer = new ProducerBuilder<Null, string>(config).Build();
    }

    public void Dispose()
    {
        NotifyProducer.Dispose();
    }
}