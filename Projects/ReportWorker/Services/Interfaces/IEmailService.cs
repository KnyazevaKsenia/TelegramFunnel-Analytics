using CommonRabbitMq;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

public interface IEmailService
{
    public Task<bool> SendReportAsync(ReportTask reportTask, ReportResult reportResult, ReportFormat format);
    public Task<bool> SendErrorAsync(ReportTask reportTask, string error);
}