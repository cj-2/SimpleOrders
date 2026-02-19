namespace SimpleOrders.Shared;

public class QueueConfig
{
    public string Name { get; set; } = string.Empty;
    public bool Durable { get; set; }
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
}

public class RabbitMqConfig
{
    public string HostName { get; set; } = string.Empty;
    public List<QueueConfig> Queues { get; set; } = [];
}