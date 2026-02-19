using RabbitMQ.Client;
using SimpleOrders.Shared;

namespace SimpleOrders.DumbRabbit;

public class Worker(ILogger<Worker> logger, RabbitMqConfig rabbitMqConfig) : BackgroundService
{
    
    private ConnectionFactory Factory { get; } = new()
    {
        HostName = rabbitMqConfig.HostName,
    };

    private IConnection? Connection { get; set; }
    private IChannel? Channel { get; set; }
    private List<string> Queues { get; } = [];
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
    }
}