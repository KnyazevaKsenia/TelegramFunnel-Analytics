using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using CommonRabbitMq;
using System.Text;
using System.Text.Json;
using CommonMongoModels;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Telegram_Analytic.Models;

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
            await _channel.QueueDeclareAsync(queue: "ai_analysis_queue", durable: true, exclusive: false, autoDelete: false);
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
            _logger.LogError(ex, " Error creating report for project {ProjectId}", createReportRequest?.ProjectId);
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
    public async Task<Guid?> CreateAiAnalysisAsync(CreateAiAnalysisRequest request)
    {
        try
        {
            if (_channel == null)
            {
                await StartAsync();
            }

            if (_channel == null)
            {
                _logger.LogError("RabbitMQ channel is not initialized");
                return null;
            }

            var analysisId = Guid.NewGuid();

            var startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
            var endDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

            var status = new AiAnalysisStatus
            {
                AnalysisId = analysisId,
                ProjectId = request.ProjectId,
                StartDate = startDate,
                EndDate = endDate,
                Status = "В очереди",
                CreatedAt = DateTime.UtcNow
            };

            await _context.AiAnalysisStatuses.InsertOneAsync(status);

            var task = new AiAnalysisTask
            {
                AnalysisId = analysisId,
                ProjectId = request.ProjectId,
                StartDate = startDate,
                EndDate = endDate,
                CreatedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(task);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: "ai_analysis_queue",
                mandatory: false,
                body: bytes);

            _logger.LogInformation(
                "AI analysis task {AnalysisId} created for project {ProjectId}",
                analysisId,
                request.ProjectId);

            return analysisId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating AI analysis for project {ProjectId}",
                request.ProjectId);

            return null;
        }
    }
    public async Task<AiAnalysisStatus?> GetAiAnalysisAsync(Guid analysisId)
    {
        var filter = Builders<AiAnalysisStatus>.Filter.Eq(x => x.AnalysisId, analysisId);

        return await _context.AiAnalysisStatuses
            .Find(filter)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AiAnalysisStatus>> GetProjectAiAnalysisStatusesAsync(Guid projectId)
    {
        var filter = Builders<AiAnalysisStatus>.Filter.Eq(x => x.ProjectId, projectId);

        return await _context.AiAnalysisStatuses
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(10)
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