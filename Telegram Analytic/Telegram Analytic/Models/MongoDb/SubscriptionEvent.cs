using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram_Analytic.Models.MongoDb;

public class SubscriptionEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("projectId")]
    public Guid ProjectId { get; set; }

    [BsonElement("telegramUserId")]
    public long TelegramUserId { get; set; }
    
    [BsonElement("action")]
    public string Action { get; set; } 
    
    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("clickToken")]
    public string ClickToken { get; set; } 

    [BsonElement("username")]
    public string Username { get; set; }

}