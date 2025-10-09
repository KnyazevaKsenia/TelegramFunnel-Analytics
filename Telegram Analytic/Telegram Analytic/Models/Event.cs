namespace Telegram_Analytic.Infrastructure.Entities;

public class Event
{
    public int Id { get; set; }         
    public required string RefCode { get; set; } 
    public required string Type { get; set; }   // 'click' / 'subscription'
    public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
    public required string Ip { get; set; } 
    public required string UserAgent { get; set; } 
}