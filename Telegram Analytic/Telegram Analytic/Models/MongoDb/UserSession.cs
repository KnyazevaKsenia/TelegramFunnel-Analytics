using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram_Analytic.Models.ClickAnalyticModels;

public class UserSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    public string SessionToken { get; set; }
    public Guid LinkId { get; set; }
    public Guid ProjectId { get; set; }
    public long? TelegramUserId { get; set; }
    public string TelegramUsername { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
    public bool IsCompleted { get; set; } = false;
}