using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;

namespace SimpleOrders.Shared.Services;

public class KafkaService(IConfiguration configuration) : IDisposable
{
    private IProducer<Null, string>? NotifyProducer { get; set; }
    private List<string> Topics { get; } = [];

    public async Task Configure()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServer"]
        };

        NotifyProducer = new ProducerBuilder<Null, string>(config).Build();
        // Aqui fiz manual, mas poderia estar nas configurações.
        await HandleTopic("tp-create-orders");
    }

    private async Task HandleTopic(string name)
    {
        if (Topics.Contains(name)) return;

        try
        {
            var adminClientConfig = new AdminClientConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServer"]
            };

            using var adminClient = new AdminClientBuilder(adminClientConfig).Build();
            await adminClient.CreateTopicsAsync([new TopicSpecification { Name = name }]);
            Topics.Add(name);
        }
        catch (CreateTopicsException e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task SendMessage(string topic, string message)
    {
        if (NotifyProducer == null) await Configure();
        await HandleTopic(topic);
        await NotifyProducer!.ProduceAsync(topic, new Message<Null, string> { Value = message });
    }

    public IConsumer<TKey, TValue> CreateConsume<TKey, TValue>(string groupId, string? autoOffsetReset = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServer"],
            GroupId = groupId,
            EnableAutoCommit = false,
            AutoOffsetReset = string.IsNullOrEmpty(autoOffsetReset)
                ? AutoOffsetReset.Earliest
                : Enum.Parse<AutoOffsetReset>(autoOffsetReset)
        };

        return new ConsumerBuilder<TKey, TValue>(config).Build();
    }

    public void Dispose()
    {
        NotifyProducer?.Dispose();
    }
}