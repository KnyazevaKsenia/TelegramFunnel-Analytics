using CommonRabbitMq;
using StatisticLibrary.Models.StatisticModels;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

public interface IPdfGenerator
{
    Task<ReportResult> GeneratePdfReportAsync(ReportTask task, ProjectStatistics statistics);
}
