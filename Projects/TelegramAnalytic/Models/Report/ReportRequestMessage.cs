namespace Telegram_Analytic.Models.Report;

public class ReportRequestMessage
{
    public Guid ReportId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportFormat Format { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Email { get; set; }
}

public enum ReportFormat
{
    Pdf,
    Excel
}

