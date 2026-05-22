namespace TelegramFunnelAnalytics.ReportWorker.Settings;

public class PolzaAiSettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "openai/gpt-4o";
    public string BaseUrl { get; set; } = "https://polza.ai/api/v1/";
}