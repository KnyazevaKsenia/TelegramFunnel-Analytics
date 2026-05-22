namespace TelegramFunnelAnalytics.ReportWorker;

public class AiReportContent
{
    public string ExecutiveSummary { get; set; } = "";
    public List<string> KeyFindings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> Risks { get; set; } = new();
}

