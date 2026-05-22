using System.Text;
using System.Text.Json;
using CommonRabbitMq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TelegramFunnelAnalytics.ReportWorker.Services.Implementations;

namespace TelegramFunnelAnalytics.ReportWorker;

public class AiAnalysisQueueConsumer : BackgroundService
{
    private const string QueueName = "ai_analysis_queue";

    private readonly ConnectionFactory _factory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiAnalysisQueueConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public AiAnalysisQueueConsumer(
        IOptions<RabbitMqSettings> options,
        IServiceScopeFactory scopeFactory,
        ILogger<AiAnalysisQueueConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var settings = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = settings.HostName ?? "localhost",
            UserName = settings.UserName ?? "guest",
            Password = settings.Password ?? "guest",
            VirtualHost = settings.VirtualHost ?? "/"
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _connection = await _factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                await HandleMessageAsync(ea, stoppingToken);
            };

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "ReportWorker слушает очередь {QueueName}",
                QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Нормальная остановка сервиса
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка consumer очереди {QueueName}", QueueName);
        }
    }

    private async Task HandleMessageAsync(
        BasicDeliverEventArgs ea,
        CancellationToken cancellationToken)
    {
        if (_channel == null)
            return;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            var task = JsonSerializer.Deserialize<AiAnalysisTask>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (task == null)
            {
                _logger.LogWarning("Получена пустая задача AI-анализа");

                await _channel.BasicAckAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellationToken);

                return;
            }

            _logger.LogInformation(
                "Получена задача AI-анализа {AnalysisId} для проекта {ProjectId}",
                task.AnalysisId,
                task.ProjectId);

            using var scope = _scopeFactory.CreateScope();

            var coordinator = scope.ServiceProvider
                .GetRequiredService<AiAnalysisCoordinator>();

            await coordinator.GenerateAiAnalysisAsync(task);

            await _channel.BasicAckAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки сообщения AI-анализа");

            await _channel.BasicNackAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync(cancellationToken);

        if (_connection != null)
            await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}