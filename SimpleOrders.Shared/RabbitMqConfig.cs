namespace SimpleOrders.Shared;

public class BindExchange
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; }
}

public class Queue
{
    public string Name { get; set; } = string.Empty;
    public bool Durable { get; set; }
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
    public List<BindExchange> Exchanges { get; set; } = [];
}

public class Exchange
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; }
}


public class RabbitMqConfig
{
    public string HostName { get; set; } = string.Empty;
    public List<Queue> Queues { get; set; } = [];
    public List<Exchange> Exchanges { get; set; } = [];
}
