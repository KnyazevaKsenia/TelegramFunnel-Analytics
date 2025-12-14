using System.Text;
using System.Text.Json;
using CommonRabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

namespace TelegramFunnelAnalytics.ReportWorker;
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqService _rabbitMqService;
    private readonly IServiceProvider _serviceProvider;
    
    public Worker(
        ILogger<Worker> logger,
        RabbitMqService rabbitMqService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _rabbitMqService = rabbitMqService;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Worker запущен");
        
        try
        {
            var channel = GetRabbitMqChannel();
            
            var pdfConsumer = new AsyncEventingBasicConsumer(channel);
            pdfConsumer.ReceivedAsync += async (model, ea) =>
            {
                await ProcessReportMessageAsync(ea, ReportFormat.Pdf, stoppingToken);
            };
            
            var excelConsumer = new AsyncEventingBasicConsumer(channel);
            excelConsumer.ReceivedAsync += async (model, ea) =>
            {
                await ProcessReportMessageAsync(ea, ReportFormat.Excel, stoppingToken);
            };
            
            await channel.BasicConsumeAsync(
                queue: "pdf_report_queue",
                autoAck: false,
                consumer: pdfConsumer);
            
            await channel.BasicConsumeAsync(
                queue: "excel_report_queue", 
                autoAck: false,
                consumer: excelConsumer);
            
            _logger.LogInformation("Подключено к очередям RabbitMQ: pdf_report_queue, excel_report_queue");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критическая ошибка в Report Worker");
            throw;
        }
    }
    
    private async Task ProcessReportMessageAsync(
        BasicDeliverEventArgs ea,
        ReportFormat format,
        CancellationToken cancellationToken)
    {
        var channel = GetRabbitMqChannel();
        var reportId = Guid.Empty;
        
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var reportTask = JsonSerializer.Deserialize<ReportTask>(message);
            
            if (reportTask == null)
            {
                _logger.LogError("Не удалось десериализовать задачу отчета");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }
            
            reportId = reportTask.ReportId;
            _logger.LogInformation("Получена задача отчета {ReportId} для формата {Format} из очереди", 
                reportId, format);
            
            using var scope = _serviceProvider.CreateScope();
            var coordinator = scope.ServiceProvider.GetRequiredService<IReportCoordinator>();
            await coordinator.GenerateReportAsync(reportTask, format);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки отчета {ReportId}", reportId);
            
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }
    
    private IChannel GetRabbitMqChannel()
    {
        var channelField = typeof(RabbitMqService).GetField("_channel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (channelField?.GetValue(_rabbitMqService) is not IChannel channel || channel.IsClosed)
            throw new InvalidOperationException("Канал RabbitMQ не доступен или закрыт");
        
        return channel;
    }
}