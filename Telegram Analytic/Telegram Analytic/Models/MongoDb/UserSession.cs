using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram_Analytic.Models.ClickAnalyticModels;

public class UserSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    [BsonElement("sessionToken")]
    [BsonRequired]
    public string SessionToken { get; set; }

    [BsonElement("linkId")]
    [BsonRequired]
    [BsonRepresentation(BsonType.String)]
    public Guid LinkId { get; set; }

    [BsonElement("projectId")]
    [BsonRequired]
    [BsonRepresentation(BsonType.String)]
    public Guid ProjectId { get; set; }

    [BsonElement("telegramUserId")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.Int64)]
    public long? TelegramUserId { get; set; }

    [BsonElement("telegramUsername")]
    [BsonIgnoreIfNull]
    public string TelegramUsername { get; set; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonRepresentation(BsonType.DateTime)]
    [BsonRequired]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonRepresentation(BsonType.DateTime)]
    [BsonRequired]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);

    [BsonElement("isCompleted")]
    [BsonDefaultValue(false)]
    public bool IsCompleted { get; set; } = false;
}