using System.Text;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace SimpleOrders.Shared.Services;

public class RabbitMessage(string key, string body, string exchange = "")
{
    public string Key { get; set; } = key;
    public string Body { get; set; } = body;
    public string Exchange { get; set; } = exchange;
}

public class RabbitMqService : IDisposable
{
    private RabbitMqSettings RabbitMqSettings { get; set; }
    private ConnectionFactory? ConnectionFactory { get; set; }
    private IConnection? Connection { get; set; }
    public IChannel? Channel { get; set; }
    private List<string> Queues { get; } = [];
    private List<string> Exchanges { get; } = [];
    private List<RabbitMessage> MessagesToRecovery { get; set; } = [];
    private bool IsTryingToConnectAndConfig { get; set; }
    private bool IsRecoveringMessages { get; set; }
    private CancellationToken GeneralCancellationToken { get; set; }
    private static ILogger<RabbitMqService> Logger { get; set; }

    private readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 4, Delay = TimeSpan.FromSeconds(2), OnRetry =
                _ =>
                {
                    Logger.LogInformation($"RabbitMQ => {DateTimeOffset.Now} - Nova tentativa de conexão.");
                    return new ValueTask();
                }
        })
        .Build();

    public RabbitMqService(
        RabbitMqSettings rabbitMqSettings,
        ILogger<RabbitMqService> logger,
        CancellationToken generalCancellationToken = new()
    )
    {
        Logger = logger;
        RabbitMqSettings = rabbitMqSettings;
        GeneralCancellationToken = generalCancellationToken;
        Task.Run(Configure, GeneralCancellationToken);
        Task.Run(RecoveringMessagesLoop, GeneralCancellationToken);
    }

    private async Task Configure()
    {
        Logger.LogInformation($"RabbitMQ => {DateTimeOffset.Now} - Iniciando configuração do serviço.");
        IsTryingToConnectAndConfig = true;

        try
        {
            ConnectionFactory ??= new ConnectionFactory { HostName = RabbitMqSettings.HostName };
            if (Connection == null)
            {
                await Pipeline.ExecuteAsync(async token =>
                {
                    Connection ??= await ConnectionFactory.CreateConnectionAsync(token);
                    Logger.LogInformation($"RabbitMQ => {DateTimeOffset.Now} - Conexão feita com o servidor.");
                }, GeneralCancellationToken);
            }

            Channel ??= await Connection!.CreateChannelAsync(cancellationToken: GeneralCancellationToken);

            foreach (var exchange in RabbitMqSettings.Exchanges.Where(exchange => !Exchanges.Contains(exchange.Name)))
            {
                await Channel.ExchangeDeclareAsync(exchange.Name, exchange.Type, true,
                    cancellationToken: GeneralCancellationToken);

                Queues.Add(exchange.Name);
            }

            foreach (var queue in RabbitMqSettings.Queues.Where(queue => !Queues.Contains(queue.Name)))
            {
                await Channel.QueueDeclareAsync(
                    queue: queue.Name,
                    durable: queue.Durable,
                    exclusive: queue.Exclusive,
                    autoDelete: queue.AutoDelete,
                    arguments: null,
                    cancellationToken: GeneralCancellationToken
                );

                foreach (var exchange in queue.Exchanges)
                {
                    await Channel.QueueBindAsync(queue.Name, exchange.Name, exchange.Key,
                        cancellationToken: GeneralCancellationToken);
                }

                Queues.Add(queue.Name);
            }

            Logger.LogInformation($"RabbitMQ => {DateTimeOffset.Now} - Configuração concluída.");
            IsTryingToConnectAndConfig = false;
        }
        catch (Exception err)
        {
            Logger.LogError($"RabbitMQ => {DateTimeOffset.Now} - Erro ao tentar configurar o serviço.");
            Task.Run(Configure, GeneralCancellationToken);
        }
    }

    private async void RecoveringMessagesLoop()
    {
        while (true)
        {
            GeneralCancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (MessagesToRecovery.Count > 0 && !IsTryingToConnectAndConfig)
                {
                    Logger.LogInformation($"RabbitMQ => {DateTimeOffset.Now} - Iniciando recuperação de  mensagens.");
                    await RecoveryMessages();
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            catch
            {
                Logger.LogError(
                    $"RabbitMQ => {DateTimeOffset.Now} - Ocorreu um erro na recuperação das mensagens.");
            }
        }
    }

    public void PublishAndForget(RabbitMessage message, CancellationToken stoppingToken = new())
    {
        Task.Run(() => Publish(message), stoppingToken);
    }

    public async Task Publish(RabbitMessage message, bool recovery = false)
    {
        try
        {
            if (IsTryingToConnectAndConfig && recovery)
            {
                throw new Exception(
                    $"RabbitMQ => {DateTimeOffset.Now} - Não foi possível enviar a mensagem já que não há conexão.");
            }

            if (IsTryingToConnectAndConfig || Connection is null || Channel is null)
            {
                MessagesToRecovery.Add(message);
                Logger.LogInformation(
                    $"RabbitMQ => {DateTimeOffset.Now} - Mensagem adicionada à lista de recuperação.");
                return;
            }

            if (!Queues.Contains(message.Key))
                throw new Exception("Informe uma fila válida.");

            var body = Encoding.UTF8.GetBytes(message.Body);
            await Channel!.BasicPublishAsync(message.Exchange, message.Key, body, GeneralCancellationToken);
            Logger.LogInformation($"RabbitMQ => {DateTimeOffset.Now} - Mensagem enviada. Recuperação? " +
                                  (recovery ? "Sim" : "Não"));
        }
        catch (AlreadyClosedException)
        {
            MessagesToRecovery.Add(message);

            if (!IsTryingToConnectAndConfig)
            {
                Logger.LogError(
                    $"RabbitMQ => {DateTimeOffset.Now} - Conexão perdida, mensagem adicionada à lista de recuperação enquanto a configuração é feita.");
                Dispose();
                Clear();
                Task.Run(Configure, GeneralCancellationToken);
            }
        }
    }

    private async Task RecoveryMessages()
    {
        if (IsTryingToConnectAndConfig || IsRecoveringMessages || MessagesToRecovery.Count == 0) return;

        IsRecoveringMessages = true;
        Logger.LogInformation(
            $"RabbitMQ => {DateTimeOffset.Now} - Mensagens a serem recuperadas: " + MessagesToRecovery.Count);

        var messagesToRecovery = new RabbitMessage[MessagesToRecovery.Count];
        MessagesToRecovery.CopyTo(messagesToRecovery);

        foreach (var message in messagesToRecovery)
        {
            try
            {
                await Publish(message, recovery: true);
                MessagesToRecovery.Remove(message);
                Logger.LogInformation(
                    $"RabbitMQ => {DateTimeOffset.Now} - Mensagem recuperada enviada, mensagens restantes: " +
                    MessagesToRecovery.Count);
            }
            catch
            {
                // ...
            }
        }

        IsRecoveringMessages = false;
    }

    private void Clear()
    {
        Channel = null;
        Connection = null;
    }

    public void Dispose()
    {
        Channel?.Dispose();
        Connection?.Dispose();
    }
}