using Telegram_Analytic.Models.ProjectModels;

namespace Telegram_Analytic.Models;

public class ReportRequestMessage
{
    public Guid ReportId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportFormat Format { get; set; }
    public string RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Mail { get; set; }
}