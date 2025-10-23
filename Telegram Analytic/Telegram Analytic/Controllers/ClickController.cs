using System.Net;
using Microsoft.AspNetCore.Mvc;
using Telegram_Analytic.Infrastructure.Database;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models.MongoDb;

namespace Telegram_Analytic.Controllers;

[Route("click")] 
public class ClickController : ControllerBase
{
    private readonly ITrackingLinksService _trackingService;
    private readonly MongoDbContext _mongoContext;
    private readonly IConfiguration _configuration;
    private readonly ITgBotService _tgBotService;
    
    public ClickController(ITrackingLinksService trackingService, MongoDbContext mongoContext, IConfiguration configuration)
    {
        _trackingService = trackingService;
        _mongoContext = mongoContext;
        _configuration = configuration;
    }
    
    [HttpGet("{identifier}")]
    public async Task<IActionResult> TrackClick(string identifier)
    {
        var trackingLink = await _trackingService.ProcessClickAsync(identifier);
        if (trackingLink == null) return Redirect(_configuration["Tracking:Domain"]);
        
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
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            SessionToken = sessionToken,
            UtmSource = decodedUtmSource,    
            UtmCampaign = decodedUtmCampaign,
            UtmContent = decodedUtmContent,
            Timestamp = DateTime.UtcNow
        };
        
        await _mongoContext.Clicks.InsertOneAsync(clickEvent);
        
        var botUsername = _configuration["Telegram:BotUsername"] ?? "your_bot";
        var botUrl = $"https://t.me/{botUsername}?start={sessionToken}";
        
        return Redirect(botUrl);
    }
    
    private string GetClientIpAddress()
    {
        
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
    
        if (Request.Headers.ContainsKey("X-Real-IP"))
            return Request.Headers["X-Real-IP"].ToString();
        
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            if (remoteIp.IsIPv4MappedToIPv6)
                return remoteIp.MapToIPv4().ToString();
        
            if (remoteIp.ToString() == "::1")
                return "127.0.0.1";
            
            return remoteIp.ToString();
        }
        
        return "unknown";
    }
}

