using CommonMongoModels;

namespace CommonRabbitMq;

public interface IRabbitMqService
{
    public Task<bool> CreateReportAsync(CreateReportRequest createReportRequest);
    public Task<List<ReportStatus>> GetProjectReportStatusesAsync(Guid projectId);
    public Task StartAsync();
}