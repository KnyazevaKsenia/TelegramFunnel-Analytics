using Telegram_Analytic.Models;

namespace Telegram_Analytic.Infrastructure.Interfaces;

public interface IMongoClickService
{
    public Task<string> TrackClick(TrackingLink trackingLink, string ipAddress, string userAgent);
    Task<Dictionary<Guid, long>> GetClickCountsForLinksAsync(IEnumerable<Guid> linkIds);
    public Task<long> GetClickCountForLink(Guid linkId);
}