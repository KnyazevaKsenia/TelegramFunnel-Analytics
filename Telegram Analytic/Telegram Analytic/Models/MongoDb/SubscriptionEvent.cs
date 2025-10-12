using MongoDB.Bson;

namespace Telegram_Analytic.Models.MongoDb;

public class SubscriptionEvent
{
    public ObjectId Id { get; set; }
    public Guid ProjectId { get; set; }
    public long TelegramUserId { get; set; }
    public string Action { get; set; } // "subscribe", "unsubscribe"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}