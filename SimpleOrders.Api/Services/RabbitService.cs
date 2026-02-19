using System.Text;
using RabbitMQ.Client;
using SimpleOrders.Shared;

namespace SimpleOrders.Api.Services;

public class RabbitService(RabbitMqConfig rabbitMqConfig) : IDisposable
{
    private ConnectionFactory Factory { get; } = new()
    {
        HostName = rabbitMqConfig.HostName,
    };

    private IConnection? Connection { get; set; }
    private IChannel? Channel { get; set; }
    private List<string> Queues { get; } = [];

    private async Task Configure(string[] queues)
    {
        Connection ??= await Factory.CreateConnectionAsync();
        Channel ??= await Connection.CreateChannelAsync();

        if (Queues.Count <= 0)
        {
            foreach (var queue in rabbitMqConfig.Queues.Where(queue => !Queues.Contains(queue.Name)))
            {
                await Channel.QueueDeclareAsync(
                    queue: queue.Name,
                    durable: queue.Durable,
                    exclusive: queue.Exclusive,
                    autoDelete: queue.AutoDelete,
                    arguments: null
                );

                Queues.Add(queue.Name);
            }
        }
    }

    public async Task Publish(string key, string message, string exchange = "")
    {
        if (Channel is null || Connection is null)
            await Configure([key]);

        if (!Queues.Contains(key))
            throw new Exception("Informe uma fila válida.");

        var body = Encoding.UTF8.GetBytes(message);
        await Channel!.BasicPublishAsync(exchange: exchange, routingKey: key, body: body);
    }

    public void Dispose()
    {
        Channel?.Dispose();
        Connection?.Dispose();
    }
}