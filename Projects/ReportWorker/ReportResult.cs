namespace TelegramFunnelAnalytics.ReportWorker;

public class ReportResult
{
    public Guid ReportId { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    public byte[]? FileBytes { get; set; }
    public long FileSize { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
