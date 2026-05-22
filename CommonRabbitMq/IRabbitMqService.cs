using CommonMongoModels;
using Telegram_Analytic.Models;

namespace CommonRabbitMq;

public interface IRabbitMqService
{
    public Task<bool> CreateReportAsync(CreateReportRequest createReportRequest);
    public Task<List<ReportStatus>> GetProjectReportStatusesAsync(Guid projectId);
    public Task StartAsync();
    Task<Guid?> CreateAiAnalysisAsync(CreateAiAnalysisRequest request);
    
    Task<AiAnalysisStatus?> GetAiAnalysisAsync(Guid analysisId);

    Task<List<AiAnalysisStatus>> GetProjectAiAnalysisStatusesAsync(Guid projectId);
}

