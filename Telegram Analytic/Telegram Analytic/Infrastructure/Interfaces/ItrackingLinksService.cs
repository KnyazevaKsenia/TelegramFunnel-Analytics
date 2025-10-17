using Telegram_Analytic.Models;

namespace Telegram_Analytic.Infrastructure.Interfaces;

public interface ITrackingLinksService
{
    public TrackingLink CreateTrackingLink(string name,
        string baseUrl,
        Guid projectId,
        string utmSource,
        string utmCampaign,
        string utmContent);
    
    public string GenerateUrlIdentifier(string name);
    
    public string GenerateUtmCampaign(string name);
    
    public string GenerateFullUrl(string baseUrl, string urlIdentifier, string utmSource, string utmCampaign,
        string utmContent);

    public Task<TrackingLink> ProcessClickAsync(string identifier);

}