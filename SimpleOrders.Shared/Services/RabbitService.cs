using System.Text;
using RabbitMQ.Client;

namespace SimpleOrders.Shared.Services;

public class RabbitService(RabbitMqConfig rabbitMqConfig) : IDisposable
{
    private ConnectionFactory? ConnectionFactory { get; set; }
    private IConnection? Connection { get; set; }
    public IChannel? Channel { get; set; }
    private List<string> Queues { get; } = [];

    public async Task Configure()
    {
        ConnectionFactory ??= new ConnectionFactory { HostName = rabbitMqConfig.HostName };
        Connection ??= await ConnectionFactory.CreateConnectionAsync();
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
            await Configure();

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