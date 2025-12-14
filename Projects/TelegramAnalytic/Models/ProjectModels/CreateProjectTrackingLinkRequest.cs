namespace Telegram_Analytic.Models.ProjectModels;

public class CreateProjectTrackingLinkRequest
{
    public required string ProjectId { get; set; }
    public required string Name { get; set; }
    public required string UtmSource { get; set; }
    public required string UtmCampaign { get; set; }
    public required string UtmContent { get; set; }
}
