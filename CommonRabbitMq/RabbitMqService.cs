using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using CommonRabbitMq;
using System.Text;
using System.Text.Json;
using CommonMongoModels;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CommonRabbitMq;

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ConnectionFactory _factory;
    private readonly MongoDbContext _context;
    private readonly ILogger<RabbitMqService> _logger;
    
    public RabbitMqService(IOptions<RabbitMqSettings> options, MongoDbContext context, ILogger<RabbitMqService> logger)
    {
        _context = context;
        _logger = logger;

        if (options == null)
        {
            
            throw new ArgumentNullException(nameof(options));
        }

        var settings = options.Value;

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        _factory = new ConnectionFactory
        {
            HostName = settings.HostName ?? "localhost",
            UserName = settings.UserName ?? "guest",
            Password = settings.Password ?? "guest",
            VirtualHost = settings.VirtualHost ?? "/"
        };

    }
    
    public async Task StartAsync()
    {
        
        try
        {
            
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            
            await _channel.QueueDeclareAsync("pdf_report_queue", durable: true, exclusive: false, autoDelete: true);
            await _channel.QueueDeclareAsync("excel_report_queue", durable: true, exclusive: false, autoDelete: true);
            await _channel.QueueDeclareAsync("report_status_queue", durable: false, exclusive: false, autoDelete: true);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    public async Task<bool> CreateReportAsync(CreateReportRequest createReportRequest)
    {
        try
        {
            Console.WriteLine($"Creating report: {createReportRequest}");
            Console.WriteLine($"{createReportRequest.StartDate}, {createReportRequest.EndDate}, {createReportRequest.Email}, {createReportRequest.ProjectId}");
            var queueName = createReportRequest.Format switch
            {
                "Pdf"   => "pdf_report_queue",
                "Excel" => "excel_report_queue",
                _ => throw new ArgumentException("Unknown report type")
            };
            var reportTask = new ReportTask
            {
                ReportId = Guid.NewGuid(),
                ProjectId = Guid.Parse(createReportRequest.ProjectId) ,
                StartDate =  DateTime.SpecifyKind(createReportRequest.StartDate.AddDays(-1), DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(createReportRequest.EndDate, DateTimeKind.Utc),
                Email = createReportRequest.Email ,
            };
            
            var json = JsonSerializer.Serialize(reportTask);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            var reportStatus = new ReportStatus()
            {
                ReportId = reportTask.ReportId,
                ProjectId = Guid.Parse(createReportRequest.ProjectId),
                Format = createReportRequest.Format,
                Status = "Отправка",
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.ReportStatuses.InsertOneAsync(reportStatus);
            
            if (_channel != null)
                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    mandatory: false,
                    body: bytes);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating report for project {ProjectId}", createReportRequest?.ProjectId);
            return false;
        }
    }
    
    public async Task<List<ReportStatus>> GetProjectReportStatusesAsync(Guid projectId)
    {
        var collection = _context.ReportStatuses;
        
        var filter = Builders<ReportStatus>.Filter.Eq(s => s.ProjectId, projectId);
        
        return await collection
            .Find(filter)
            .ToListAsync();
    }
    
    public void Dispose()
    {
        _channel?.CloseAsync();
        _channel?.Dispose();
        _connection?.CloseAsync();
        _connection?.Dispose();
    }
}