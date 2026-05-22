namespace CommonRabbitMq;

public class AiAnalysisTask
{
    public Guid AnalysisId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}