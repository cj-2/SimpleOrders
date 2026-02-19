using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace SimpleOrders.Api.Services;

public class KafkaService : IDisposable
{
    public IProducer<Null, string> NotifyProducer { get; }
    private IConfiguration Configuration { get; set; }
    public List<string> Topics { get; set; } = [];

    public KafkaService(IConfiguration configuration)
    {
        Configuration = configuration;

        var config = new ProducerConfig
        {
            BootstrapServers = Configuration["Kafka:BootstrapServer"]
        };

        NotifyProducer = new ProducerBuilder<Null, string>(config).Build();
    }

    private async Task CreateTopic(string name)
    {
        if (Topics.Contains(name)) return;

        try
        {
            var adminClientConfig = new AdminClientConfig
            {
                BootstrapServers = Configuration["Kafka:BootstrapServer"]
            };

            using var adminClient = new AdminClientBuilder(adminClientConfig).Build();
            var topicSpecification = new TopicSpecification { Name = name };

            await adminClient.CreateTopicsAsync([topicSpecification]);
            Topics.Add(name);
        }
        catch (CreateTopicsException e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public async Task SendMessage(string topic, string message)
    {
        await CreateTopic(topic);
        await NotifyProducer.ProduceAsync(topic, new Message<Null, string> { Value = message });
    }

    public void Dispose()
    {
        NotifyProducer.Dispose();
    }
}