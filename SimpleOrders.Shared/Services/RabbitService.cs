using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace SimpleOrders.Shared.Services;

public class RabbitService(RabbitMqConfig rabbitMqConfig) : IDisposable
{
    private ConnectionFactory? ConnectionFactory { get; set; }
    private IConnection? Connection { get; set; }
    public IChannel? Channel { get; set; }
    private List<string> Queues { get; } = [];
    private List<string> Exchanges { get; } = [];

    public async Task Configure(CancellationToken stoppingToken = new())
    {
        ConnectionFactory ??= new ConnectionFactory { HostName = rabbitMqConfig.HostName };

        while (true)
        {
            try
            {
                Connection ??= await ConnectionFactory.CreateConnectionAsync(stoppingToken);
                Console.WriteLine("RabbitMQ: Conexão feita.");
                break;
            }
            catch (BrokerUnreachableException err)
            {
                Console.WriteLine("RabbitMQ: Erro conexão, re-tentativa em 1 segundo.");
                Thread.Sleep(1000);
            }
        }

        Channel ??= await Connection.CreateChannelAsync(cancellationToken: stoppingToken);

        foreach (var exchange in rabbitMqConfig.Exchanges.Where(exchange => !Exchanges.Contains(exchange.Name)))
        {
            await Channel.ExchangeDeclareAsync(exchange.Name, exchange.Type, true,
                cancellationToken: stoppingToken);
            
            Queues.Add(exchange.Name);
        }

        foreach (var queue in rabbitMqConfig.Queues.Where(queue => !Queues.Contains(queue.Name)))
        {
            await Channel.QueueDeclareAsync(
                queue: queue.Name,
                durable: queue.Durable,
                exclusive: queue.Exclusive,
                autoDelete: queue.AutoDelete,
                arguments: null,
                cancellationToken: stoppingToken
            );

            foreach (var exchange in queue.Exchanges)
            {
                await Channel.QueueBindAsync(queue.Name, exchange.Name, exchange.Key, cancellationToken: stoppingToken);
            }

            Queues.Add(queue.Name);
        }
    }

    public async Task Publish(string key, string message, string exchange = "")
    {
        if (Channel is null || Connection is null)
            await Configure();

        if (!Queues.Contains(key))
            throw new Exception("Informe uma fila válida.");

        var body = Encoding.UTF8.GetBytes(message);
        await Channel!.BasicPublishAsync(exchange, key, body);
    }

    public void Dispose()
    {
        Channel?.Dispose();
        Connection?.Dispose();
    }
}