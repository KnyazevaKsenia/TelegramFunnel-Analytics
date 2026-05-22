
using CommonMongoModels;
using CommonRabbitMq;
using MongoDB.Driver;
using StatisticLibrary.Interfaces;
using StatisticLibrary.Models.StatisticModels;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Implementations;

public class AiAnalysisCoordinator
{
    private readonly IProjectStatisticManager _statisticManager;
    private readonly IAiReportService _aiReportService;
    private readonly MongoDbContext _dbContext;
    private readonly ILogger<AiAnalysisCoordinator> _logger;

    public AiAnalysisCoordinator(
        IProjectStatisticManager statisticManager,
        IAiReportService aiReportService,
        MongoDbContext dbContext,
        ILogger<AiAnalysisCoordinator> logger)
    {
        _statisticManager = statisticManager;
        _aiReportService = aiReportService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task GenerateAiAnalysisAsync(AiAnalysisTask task)
    {
        var filter = Builders<AiAnalysisStatus>.Filter.Eq(
            x => x.AnalysisId,
            task.AnalysisId);

        try
        {
            _logger.LogInformation(
                "Начата обработка AI-анализа {AnalysisId} для проекта {ProjectId}",
                task.AnalysisId,
                task.ProjectId);

            await _dbContext.AiAnalysisStatuses.UpdateOneAsync(
                filter,
                Builders<AiAnalysisStatus>.Update
                    .Set(x => x.Status, "Обработка запроса"));

            var statistics = await _statisticManager.GetProjectStatsAsync(new StatFilter
            {
                ProjectId = task.ProjectId,
                StartDate = task.StartDate,
                EndDate = task.EndDate
            });

            var reportTask = new ReportTask
            {
                ReportId = task.AnalysisId,
                ProjectId = task.ProjectId,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Email = ""
            };

            var aiContent = await _aiReportService.GenerateReportContentAsync(
                reportTask,
                statistics);

            await _dbContext.AiAnalysisStatuses.UpdateOneAsync(
                filter,
                Builders<AiAnalysisStatus>.Update
                    .Set(x => x.Status, "Готово")
                    .Set(x => x.Content, aiContent)
                    .Set(x => x.CompletedAt, DateTime.UtcNow)
                    .Set(x => x.ErrorMessage, null));

            _logger.LogInformation(
                "AI-анализ {AnalysisId} успешно готов",
                task.AnalysisId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ошибка при обработке AI-анализа {AnalysisId}",
                task.AnalysisId);

            await _dbContext.AiAnalysisStatuses.UpdateOneAsync(
                filter,
                Builders<AiAnalysisStatus>.Update
                    .Set(x => x.Status, "Ошибка")
                    .Set(x => x.ErrorMessage, ex.Message)
                    .Set(x => x.CompletedAt, DateTime.UtcNow));
        }
    }
}