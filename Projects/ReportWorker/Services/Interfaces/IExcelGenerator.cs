using CommonRabbitMq;
using StatisticLibrary.Models.StatisticModels;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

public interface IExcelGenerator
{
    Task<ReportResult> GenerateExcelReportAsync(ReportTask task, ProjectStatistics statistics);
    byte[] GenerateExcelBytes(ProjectStatistics statistics);
}