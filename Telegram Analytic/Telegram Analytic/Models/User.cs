namespace Telegram_Analytic.Infrastructure.Entities;

public class User
{
    public int Id { get; set; }    
    public required long TelegramId { get; set; }    
    public required string Name { get; set; }   
    public required string ApiToken { get; set; } 
    // Связь с кампаниями
    public ICollection<Link> Links { get; set; } = new List<Link>();
}
