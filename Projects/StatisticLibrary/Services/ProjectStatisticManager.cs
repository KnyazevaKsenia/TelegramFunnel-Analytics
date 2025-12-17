using CommonMongoModels;
using MongoDB.Driver;
using StatisticLibrary.Models.StatisticModels;
using StatisticLibrary.Interfaces;


namespace StatisticLibrary.Services;

public class ProjectStatisticManager : IProjectStatisticManager
{
    private readonly IMongoCollection<ClickEvent> _clicks;
    private readonly IIpLocationService _locationService;
    
    public ProjectStatisticManager(IMongoCollection<ClickEvent> clicks,  IIpLocationService ipLocationService)
    {
        _clicks = clicks;
        _locationService = ipLocationService;
    }
    
    public async Task<ProjectStatistics> GetProjectStatsAsync(StatFilter filter)
    {
        try
        {
            var events = await GetFilteredEventsAsync(filter);
            
            if (!events.Any())
                return CreateEmptyStats(filter);
            
            var stats = new ProjectStatistics
            {
                ProjectId = filter.ProjectId,
                PeriodStart = filter.StartDate ?? events.Min(x => x.Timestamp),
                PeriodEnd = filter.EndDate ?? events.Max(x => x.Timestamp),
                TotalClicks = events.Count,
                TotalSubscriptions = events.Count(x => x.IsSubscribed),
                UniqueUsers = events.Select(x => x.UserId).Distinct().Count(),
                DailyStats = GetDailyStats(events),
                SourceStats = GetSourceStats(events),
                CampaignStats = GetCampaignStats(events),
                ContentStats = GetContentStats(events), 
                LocationStats = await GetLocationStatsAsync(events), 
                DeviceStats = GetDeviceStats(events)
            };
            
            stats.ConversionRate = stats.TotalClicks > 0 
                ? Math.Round((double)stats.TotalSubscriptions / stats.TotalClicks * 100, 2)
                : 0;

            return stats;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    public async Task<object> GetChartDataAsync(StatFilter filter, string chartType)
    {
        var events = await GetFilteredEventsAsync(filter);
    
        return chartType.ToLower() switch
        {
            "daily" => GetDailyStats(events),
            "sources" => GetSourceStats(events),
            "campaigns" => GetCampaignStats(events),
            "content" => GetContentStats(events), 
            "locations" => await GetLocationStatsAsync(events),
            "devices" => GetDeviceStats(events),
            _ => throw new ArgumentException($"Unknown chart type: {chartType}")
        };
    }
    private List<ContentStat> GetContentStats(List<ClickEvent> events)
    {
        return events
            .GroupBy(x => x.UtmContent ?? "unknown")
            .Select(g => new ContentStat
            {
                Content = g.Key,
                Clicks = g.Count(),
                Subscriptions = g.Count(x => x.IsSubscribed)
            })
            .OrderByDescending(x => x.Clicks)
            .ToList();
    }
    
    private async Task<List<LocationStat>> GetLocationStatsAsync(List<ClickEvent> events)
    {
        var locationStats = new List<LocationStat>();
        
        var ipGroups = events
            .Where(x => !string.IsNullOrEmpty(x.IpAddress))
            .GroupBy(x => x.IpAddress)
            .ToList();
        
        foreach (var ipGroup in ipGroups)
        {
            var location = await _locationService.GetLocationAsync(ipGroup.Key);
            var locationKey = $"{location.Country}|{location.City}";
            
            var stat = locationStats.FirstOrDefault(x => 
                x.Country == location.Country && x.City == location.City);
                
            if (stat == null)
            {
                stat = new LocationStat
                {
                    Country = location.Country,
                    City = location.City
                };
                locationStats.Add(stat);
            }
            
            stat.Clicks += ipGroup.Count();
            stat.Subscriptions += ipGroup.Count(x => x.IsSubscribed);
        }
        
        return locationStats
            .OrderByDescending(x => x.Clicks)
            .ToList();
    }
    
    private List<DeviceStat> GetDeviceStats(List<ClickEvent> events)
    {
        var deviceStats = events
            .Select(x => new
            {
                DeviceType = GetDeviceType(x.UserAgent),
                Browser = GetBrowser(x.UserAgent)
            })
            .GroupBy(x => new { x.DeviceType, x.Browser })
            .Select(g => new DeviceStat
            {
                DeviceType = g.Key.DeviceType,
                Browser = g.Key.Browser,
                Clicks = g.Count(),
                Subscriptions = events.Count(e => 
                    GetDeviceType(e.UserAgent) == g.Key.DeviceType && 
                    GetBrowser(e.UserAgent) == g.Key.Browser && 
                    e.IsSubscribed)
            })
            .OrderByDescending(x => x.Clicks)
            .ToList();
            
        return deviceStats;
    }
    
    private string GetDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        
        var ua = userAgent.ToLower();
        
        
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";
        if (ua.Contains("iphone"))
            return "IPhone";
        if (ua.Contains("android"))
            return "Android";
        if (ua.Contains("mobile"))
            return "Mobile";
        return "Desktop";
    }
    
    private string GetBrowser(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        
        var ua = userAgent.ToLower();
        
        if (ua.Contains("chrome") && !ua.Contains("edg") && !ua.Contains("opr"))
            return "Chrome";
        if (ua.Contains("firefox"))
            return "Firefox";
        if (ua.Contains("safari") && !ua.Contains("chrome"))
            return "Safari";
        if (ua.Contains("edg"))
            return "Edge";
        if (ua.Contains("opera") || ua.Contains("opr"))
            return "Opera";
        if (ua.Contains("yabrowser"))
            return "Yandex Browser";
            
        return "Other";
    }
    
    

    private async Task<List<ClickEvent>> GetFilteredEventsAsync(StatFilter filter)
    {
        var mongoFilter = Builders<ClickEvent>.Filter.Eq(x => x.ProjectId, filter.ProjectId);

        if (filter.StartDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(filter.StartDate.Value.AddDays(-1), DateTimeKind.Utc);
            mongoFilter &= Builders<ClickEvent>.Filter.Gte(x => x.Timestamp, startDateUtc);
        }
        
        if (filter.EndDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(filter.EndDate.Value.AddDays(1), DateTimeKind.Utc);
            mongoFilter &= Builders<ClickEvent>.Filter.Lt(x => x.Timestamp, endDateUtc);
        }
        
        if (filter.Sources?.Any() == true)
            mongoFilter &= Builders<ClickEvent>.Filter.In(x => x.UtmSource, filter.Sources);

        if (filter.Campaigns?.Any() == true)
            mongoFilter &= Builders<ClickEvent>.Filter.In(x => x.UtmCampaign, filter.Campaigns);
        
        if (filter.Contents?.Any() == true) 
            mongoFilter &= Builders<ClickEvent>.Filter.In(x => x.UtmContent, filter.Contents);
        
        return await _clicks.Find(mongoFilter).ToListAsync();
    }
    
    private List<DailyStat> GetDailyStats(List<ClickEvent> events)
    {
        return events
            .GroupBy(x => x.Timestamp.Date)
            .Select(g => new DailyStat
            {
                Date = g.Key,
                Clicks = g.Count(),
                Subscriptions = g.Count(x => x.IsSubscribed)
            })
            .OrderBy(x => x.Date)
            .ToList();
    }
    
    private List<SourceStat> GetSourceStats(List<ClickEvent> events)
    {
        return events
            .GroupBy(x => x.UtmSource ?? "unknown")
            .Select(g => new SourceStat
            {
                Source = g.Key,
                Clicks = g.Count(),
                Subscriptions = g.Count(x => x.IsSubscribed)
            })
            .OrderByDescending(x => x.Clicks)
            .ToList();
    }

    private List<CampaignStat> GetCampaignStats(List<ClickEvent> events)
    {
        return events
            .GroupBy(x => x.UtmCampaign ?? "unknown")
            .Select(g => new CampaignStat
            {
                Campaign = g.Key,
                Clicks = g.Count(),
                Subscriptions = g.Count(x => x.IsSubscribed)
            })
            .OrderByDescending(x => x.Clicks)
            .ToList();
    }
    
    private ProjectStatistics CreateEmptyStats(StatFilter filter)
    {
        return new ProjectStatistics
        {
            ProjectId = filter.ProjectId,
            PeriodStart = filter.StartDate ?? DateTime.UtcNow.AddDays(-30),
            PeriodEnd = filter.EndDate ?? DateTime.UtcNow
        };
    }
    
}