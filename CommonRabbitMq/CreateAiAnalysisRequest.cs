namespace Telegram_Analytic.Models;


public class CreateAiAnalysisRequest
{
    public Guid ProjectId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}