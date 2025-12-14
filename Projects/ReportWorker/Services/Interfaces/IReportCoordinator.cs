using CommonRabbitMq;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

public interface IReportCoordinator
{
    Task GenerateReportAsync(ReportTask task, ReportFormat format);
}