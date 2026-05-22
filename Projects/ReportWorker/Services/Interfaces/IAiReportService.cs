using CommonRabbitMq;
using StatisticLibrary.Models.StatisticModels;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

public interface IAiReportService
{
    Task<AiReportContent> GenerateReportContentAsync(
        ReportTask task,
        ProjectStatistics statistics);
}