namespace Telegram_Analytic.Infrastructure.Entities;

public class Link
{
    public required int Id { get; set; }            
    public required string Title { get; set; }     
    public required string RefCode { get; set; }   
    public required string TargetUrl { get; set; } 
    public required string UtmSource { get; set; }  
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
    
    //Связь с пользователем
    public required int UserId { get; set; }
    public required User User { get; set; }
}
