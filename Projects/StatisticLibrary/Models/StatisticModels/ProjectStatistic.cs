namespace StatisticLibrary.Models.StatisticModels;

public class ProjectStatistics
{
    public Guid ProjectId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    public int TotalClicks { get; set; }
    public int TotalSubscriptions { get; set; }
    public double ConversionRate { get; set; }
    public int UniqueUsers { get; set; }
    
    public List<DailyStat> DailyStats { get; set; } = new();
    
    public List<SourceStat> SourceStats { get; set; } = new();
    
    public List<CampaignStat> CampaignStats { get; set; } = new();
    
    public List<LocationStat> LocationStats { get; set; } = new();
    public List<DeviceStat> DeviceStats { get; set; } = new();
    public List<ContentStat> ContentStats { get; set; } = new(); // НОВОЕ
}

public class ContentStat
{
    public string Content { get; set; }
    public int Clicks { get; set; }
    public int Subscriptions { get; set; }
    public double ConversionRate => Clicks > 0 ? Math.Round((double)Subscriptions / Clicks * 100, 2) : 0;
}
public class DailyStat
{
    public DateTime Date { get; set; }
    public int Clicks { get; set; }
    public int Subscriptions { get; set; }
    public double ConversionRate => Clicks > 0 ? Math.Round((double)Subscriptions / Clicks * 100, 2) : 0;
}

public class SourceStat
{
    public string Source { get; set; }
    public int Clicks { get; set; }
    public int Subscriptions { get; set; }
    public double ConversionRate => Clicks > 0 ? Math.Round((double)Subscriptions / Clicks * 100, 2) : 0;
}

public class CampaignStat
{
    public string Campaign { get; set; }
    public int Clicks { get; set; }
    public int Subscriptions { get; set; }
    public double ConversionRate => Clicks > 0 ? Math.Round((double)Subscriptions / Clicks * 100, 2) : 0;
}


public class StatFilter
{
    public Guid ProjectId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string[] Sources { get; set; }
    public string[] Campaigns { get; set; }
    public string[] Contents { get; set; } // НОВОЕ
}
public class LocationStat
{
    public string Country { get; set; }
    public string City { get; set; }
    public int Clicks { get; set; }
    public int Subscriptions { get; set; }
    public double ConversionRate => Clicks > 0 ? Math.Round((double)Subscriptions / Clicks * 100, 2) : 0;
}

public class DeviceStat
{
    public string DeviceType { get; set; }
    public string Browser { get; set; }
    public int Clicks { get; set; }
    public int Subscriptions { get; set; }
    public double ConversionRate => Clicks > 0 ? Math.Round((double)Subscriptions / Clicks * 100, 2) : 0;
}

