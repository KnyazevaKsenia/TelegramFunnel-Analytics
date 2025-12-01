using Microsoft.AspNetCore.Mvc;
using Telegram_Analytic.Infrastructure.Interfaces;


namespace Telegram_Analytic.Controllers;

[Route("click")] 
public class ClickController : ControllerBase
{
    private readonly ITrackingLinksService _trackingService;
    private readonly IConfiguration _configuration;
    private IMongoClickService _mongoClickService;
    
    public ClickController(ITrackingLinksService trackingService, IConfiguration configuration, IMongoClickService mongoClickService)
    {
        _trackingService = trackingService;
        _mongoClickService = mongoClickService;
        _configuration = configuration;
    }
    
    [HttpGet("{identifier}")]
    public async Task<IActionResult> TrackClick(string identifier)
    {
        var trackingLink = await _trackingService.ProcessClickAsync(identifier);
        if (trackingLink == null) return Redirect(_configuration["Tracking:Domain"]);
        
        var botUrl = await _mongoClickService.TrackClick(trackingLink, GetClientIpAddress(), Request.Headers["User-Agent"].ToString());
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

