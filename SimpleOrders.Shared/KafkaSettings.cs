namespace SimpleOrders.Shared;

public class TopicConfig
{
    public string Name { get; set; } = string.Empty;
}

public class KafkaSettings
{
    public string BootstrapServer { get; set; } = string.Empty;
    public List<TopicConfig> Topics { get; set; } = [];
}
