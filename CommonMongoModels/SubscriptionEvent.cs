using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonMongoModels;
 
public class SubscriptionEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    [BsonElement("telegramUserId")]
    [BsonRequired]
    [BsonRepresentation(BsonType.Int64)]
    public long TelegramUserId { get; set; }

    [BsonElement("telegramUsername")]
    [BsonIgnoreIfNull]
    public string TelegramUsername { get; set; }

    [BsonElement("sessionToken")]
    [BsonRequired]
    public string SessionToken { get; set; }
    
    [BsonElement("action")]
    [BsonRequired]
    public string Action { get; set; } // "subscribe"

    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonRepresentation(BsonType.DateTime)]
    [BsonRequired]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
