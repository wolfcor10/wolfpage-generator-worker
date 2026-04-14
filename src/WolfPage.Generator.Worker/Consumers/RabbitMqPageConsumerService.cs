using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using WolfPage.Generator.Application.Messages;
using WolfPage.Generator.Application.Services;
using WolfPage.Generator.Infrastructure.Options;

namespace WolfPage.Generator.Worker.Consumers;

public class RabbitMqPageConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqPageConsumerService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IModel? _channel;

    private const int MaxRetries = 5;

    public RabbitMqPageConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqPageConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };

        var delay = TimeSpan.FromSeconds(3);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: _options.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation(
                    "Conectado a RabbitMQ. Host={Host}:{Port}, Cola={Queue}",
                    _options.HostName, _options.Port, _options.QueueName);

                break;
            }
            catch (BrokerUnreachableException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(
                    "RabbitMQ no disponible (intento {Attempt}/{Max}). Reintentando en {Delay}s... — {Message}",
                    attempt, MaxRetries, delay.TotalSeconds, ex.Message);

                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
        }

        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel no inicializado.");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Mensaje recibido: {Message}", json);

                var message = JsonSerializer.Deserialize<CreatePageRequestedMessage>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (message is null)
                    throw new InvalidOperationException("No fue posible deserializar el mensaje.");

                using var scope = _scopeFactory.CreateScope();
                var pageGenerationService = scope.ServiceProvider.GetRequiredService<IPageGenerationService>();

                await pageGenerationService.ProcessAsync(message, stoppingToken);

                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje RabbitMQ.");
                _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();

        _channel?.Dispose();
        _connection?.Dispose();

        _logger.LogInformation("Consumer RabbitMQ detenido.");

        return base.StopAsync(cancellationToken);
    }
}
