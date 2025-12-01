using Telegram_Analytic.Models;

namespace Telegram_Analytic.Infrastructure.Interfaces;

public interface ITrackingLinksService
{
    public Task<TrackingLink> CreateTrackingLink(string name,
        Guid projectId,
        string utmSource,
        string utmCampaign,
        string utmContent);
    
    public string GenerateUrlIdentifier(string name);
    
    public string GenerateUtmCampaign(string name);
    
    public string GenerateFullUrl(string baseUrl, string urlIdentifier, string utmSource, string utmCampaign,
        string utmContent);

    public Task<TrackingLink?> ProcessClickAsync(string identifier);

}