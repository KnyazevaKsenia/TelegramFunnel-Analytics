using Telegram_Analytic.Models;
using System.Net;
using CommonMongoModels;
using MongoDB.Driver;
using Telegram_Analytic.Infrastructure.Interfaces;


namespace Telegram_Analytic.Services;

public class MongoClickService : IMongoClickService
{
    private readonly IMongoCollection<ClickEvent> _clicksCollection;
    private readonly IConfiguration _configuration;
    public MongoClickService(MongoDbContext mongoContext, IConfiguration configuration)
    {
        _clicksCollection = mongoContext.Clicks;
        _configuration = configuration;
    }
    
    public async Task<string> TrackClick(TrackingLink trackingLink,string ipAddress, string userAgent )
    {
        var utmSource = trackingLink.UtmSource;
        var utmCampaign = trackingLink.UtmCampaign;
        var utmContent = trackingLink.UtmContent;
        
        var decodedUtmSource = WebUtility.UrlDecode(utmSource); 
        var decodedUtmCampaign = WebUtility.UrlDecode(utmCampaign);
        var decodedUtmContent = WebUtility.UrlDecode(utmContent);
        
        var sessionToken = Guid.NewGuid().ToString("N").Substring(0, 12);
        
        var clickEvent = new ClickEvent
        {
            LinkId = trackingLink.Id,
            ProjectId = trackingLink.ProjectId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionToken = sessionToken,
            UtmSource = decodedUtmSource,    
            UtmCampaign = decodedUtmCampaign,
            UtmContent = decodedUtmContent,
            Timestamp = DateTime.UtcNow
        };
        
        await _clicksCollection.InsertOneAsync(clickEvent);
        
        var botUsername = _configuration["Telegram:BotUsername"] ?? "your_bot";
        var botUrl = $"https://t.me/{botUsername}?start={sessionToken}";
        return botUrl;
    }
    
    public async Task<long> GetClickCountForLink(Guid linkId)
    {
        var filter = Builders<ClickEvent>.Filter.Eq(click => click.LinkId, linkId);
        return await _clicksCollection.CountDocumentsAsync(filter);
    }
    
    public async Task<Dictionary<Guid, long>> GetClickCountsForLinksAsync(IEnumerable<Guid> linkIds)
    {
        var linkIdsList = linkIds.ToList();
        var result = await _clicksCollection
            .Aggregate()
            .Match(click => linkIdsList.Contains(click.LinkId))
            .Group(click => click.LinkId, 
                group => new { LinkId = group.Key, Count = group.LongCount() })
            .ToListAsync();
        
        return result.ToDictionary(x => x.LinkId, x => x.Count);
    }
    
}