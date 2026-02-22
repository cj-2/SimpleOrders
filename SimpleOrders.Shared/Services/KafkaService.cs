using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace SimpleOrders.Shared.Services;

public class KafkaService(KafkaConfig kafkaConfig) : IDisposable
{
    private AdminClientConfig? AdminClientConfig { get; set; }
    private ProducerConfig? ProducerConfig { get; set; }
    private IProducer<Null, string>? NotifyProducer { get; set; }
    private List<string> Topics { get; } = [];

    public async Task Configure()
    {
        ProducerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServer
        };

        NotifyProducer = new ProducerBuilder<Null, string>(ProducerConfig).Build();

        try
        {
            AdminClientConfig = new AdminClientConfig
            {
                BootstrapServers = kafkaConfig.BootstrapServer
            };

            using var adminClient = new AdminClientBuilder(AdminClientConfig).Build();

            foreach (var topic in kafkaConfig.Topics)
            {
                if (Topics.Contains(topic.Name)) continue;

                await adminClient.CreateTopicsAsync([new TopicSpecification { Name = topic.Name }]);
                Topics.Add(topic.Name);
            }
        }
        catch (CreateTopicsException e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private async Task HandleTopic(string name)
    {
        if (AdminClientConfig == null) await Configure();
        if (Topics.Contains(name)) return;

        try
        {
            using var adminClient = new AdminClientBuilder(AdminClientConfig).Build();
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
        await NotifyProducer!.ProduceAsync(topic, new Message<Null, string> { Value = message });
    }

    public IConsumer<TKey, TValue> CreateConsume<TKey, TValue>(string groupId, string? autoOffsetReset = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServer,
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