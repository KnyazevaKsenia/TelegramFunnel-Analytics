namespace Telegram_Analytic.Models.ProjectModels;

public class CreateProjectTrackingLinkRequest
{
    public string ProjectId { get; set; }
    public string Name { get; set; }
    public string BaseUrl { get; set; }
    public string UtmSource { get; set; }
    public string UtmCampaign { get; set; }
    public string UtmContent { get; set; }
}