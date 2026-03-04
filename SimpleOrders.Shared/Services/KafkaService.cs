using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Polly;
using Polly.Retry;

namespace SimpleOrders.Shared.Services;

public class KafkaService() : IDisposable
{
    private KafkaSettings KafkaSettings { get; set; }
    private CancellationToken GeneralGeneralCancellationToken { get; set; }

    private AdminClientConfig AdminClientConfig { get; set; }
    private ProducerConfig ProducerConfig { get; set; }
    private IAdminClient AdminClient { get; set; }
    private IProducer<Null, string> NotifyProducer { get; set; }

    private List<string> Topics { get; } = [];

    private readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

    public KafkaService(KafkaSettings kafkaSettings, CancellationToken generalCancellationToken = new()) : this()
    {
        KafkaSettings = kafkaSettings;
        GeneralGeneralCancellationToken = generalCancellationToken;

        ProducerConfig = new ProducerConfig
        {
            BootstrapServers = KafkaSettings.BootstrapServer,
        };

        AdminClientConfig = new AdminClientConfig
        {
            BootstrapServers = KafkaSettings.BootstrapServer,
        };

        AdminClient = new AdminClientBuilder(AdminClientConfig).Build();
        NotifyProducer = new ProducerBuilder<Null, string>(ProducerConfig).Build();

        Task.Run(Configure, GeneralGeneralCancellationToken);
    }

    private async Task Configure()
    {
        try
        {
            AdminClient.GetMetadata(TimeSpan.FromSeconds(5));

            foreach (var topic in KafkaSettings.Topics.Where(e => !Topics.Contains(e.Name)))
            {
                if (Topics.Contains(topic.Name)) continue;

                try
                {
                    await AdminClient.CreateTopicsAsync([
                        new TopicSpecification { Name = topic.Name, NumPartitions = 3 }
                    ]);

                    Topics.Add(topic.Name);
                }
                catch (CreateTopicsException err)
                {
                    Console.WriteLine(err.Message);
                    Topics.Add(topic.Name);
                }
            }
        }
        catch (Exception err)
        {
            Console.WriteLine(err.Message);
            Task.Run(Configure, GeneralGeneralCancellationToken);
        }
    }

    private async Task _SendMessage(string topic, string message, CancellationToken cancellation)
    {
        await NotifyProducer.ProduceAsync(topic, new Message<Null, string> { Value = message }, cancellation);
    }

    public async Task SendMessage(string topic, string message, CancellationToken cancellation)
    {
        await Pipeline.ExecuteAsync(async (token) => { await _SendMessage(topic, message, token); }, cancellation);
    }

    public IConsumer<TKey, TValue> CreateConsume<TKey, TValue>(string groupId, string? autoOffsetReset = null)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaSettings.BootstrapServer,
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
        NotifyProducer.Flush();
        NotifyProducer.Dispose();
        AdminClient.Dispose();
    }
}