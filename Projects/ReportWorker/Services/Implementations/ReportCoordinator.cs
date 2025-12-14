using CommonMongoModels;
using CommonRabbitMq;
using MongoDB.Driver;
using StatisticLibrary.Interfaces;
using StatisticLibrary.Models.StatisticModels;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Implementations
{
    public class ReportCoordinator : IReportCoordinator
    {
        private readonly IProjectStatisticManager _statisticManager;
        private readonly IPdfGenerator _pdfGenerator;
        private readonly IExcelGenerator _excelGenerator;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReportCoordinator> _logger;
        private readonly MongoDbContext _dbContext;
        
        public ReportCoordinator(
            IProjectStatisticManager statisticManager,
            IPdfGenerator pdfGenerator,
            IExcelGenerator excelGenerator,
            IEmailService emailService,
            ILogger<ReportCoordinator> logger,
            MongoDbContext dbContext)
        {
            _statisticManager = statisticManager;
            _pdfGenerator = pdfGenerator;
            _excelGenerator = excelGenerator;
            _emailService = emailService;
            _logger = logger;
            _dbContext = dbContext;
        }
        
        public async Task GenerateReportAsync(ReportTask task, ReportFormat format)
        {
            try
            {
                _logger.LogInformation("Координация генерации отчета {ReportId} (Формат: {Format})", 
                    task.ReportId, format);
                
                var filter = Builders<ReportStatus>.Filter.Eq(report => report.ReportId, task.ReportId);
                var status = _dbContext.ReportStatuses.Find(filter).FirstOrDefault();
                
                if (status != null)
                {
                    var update = Builders<ReportStatus>.Update
                        .Set(report => report.Status, "Обработка запроса");
                    await _dbContext.ReportStatuses.UpdateOneAsync(filter, update);
                }
                
                var statistics = await GetStatisticsAsync(task);
                
                ReportResult result = format switch
                {
                    ReportFormat.Pdf => await _pdfGenerator.GeneratePdfReportAsync(task, statistics),
                    ReportFormat.Excel => await _excelGenerator.GenerateExcelReportAsync(task, statistics),
                    _ => throw new ArgumentException($"Неподдерживаемый формат отчета: {format}")
                };
                
                if (result.IsSuccess)
                {
                    await _emailService.SendReportAsync(task, result, format);
                    
                    var update = Builders<ReportStatus>.Update
                        .Set(report => report.Status, $"Отправлен на {task.Email}");
                    await _dbContext.ReportStatuses.UpdateOneAsync(filter, update);
                    
                    _logger.LogInformation("Отчет {ReportId} отправлен на {Email}", 
                        task.ReportId, task.Email);
                }
                else
                {
                    var update = Builders<ReportStatus>.Update
                        .Set(report => report.Status, "Ошибка");
                    await _dbContext.ReportStatuses.UpdateOneAsync(filter, update);
                    await _emailService.SendErrorAsync(task, result.ErrorMessage ?? "Неизвестная ошибка");
                    _logger.LogError("Ошибка генерации отчета {ReportId}: {Error}", 
                        task.ReportId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "Критическая ошибка при обработке отчета {ReportId}", task.ReportId);
                
                await _emailService.SendErrorAsync(task, ex.Message);
                
                throw;
            }
        }
        
        private async Task<ProjectStatistics> GetStatisticsAsync(ReportTask task)
        {
            _logger.LogInformation("Получение статистики для проекта {ProjectId} за период {StartDate} - {EndDate}", 
                task.ProjectId, task.StartDate.ToString("dd.MM.yyyy"), task.EndDate.ToString("dd.MM.yyyy"));
            
            var filter = new StatFilter
            {
                ProjectId = task.ProjectId,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
            };
            
            var statistics = await _statisticManager.GetProjectStatsAsync(filter);
            
            _logger.LogInformation("Статистика получена: {Clicks} кликов, {Subscriptions} подписок, {ConversionRate:F2}% конверсия",
                statistics.TotalClicks, statistics.TotalSubscriptions, statistics.ConversionRate);
            
            return statistics;
        }
    }
}

