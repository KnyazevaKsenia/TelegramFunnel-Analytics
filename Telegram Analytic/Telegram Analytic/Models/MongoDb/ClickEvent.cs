using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Telegram_Analytic.Models.MongoDb;

public class ClickEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("linkId")]
    [BsonRepresentation(BsonType.String)]
    public Guid LinkId { get; set; }

    [BsonElement("projectId")]
    [BsonRepresentation(BsonType.String)]
    public Guid ProjectId { get; set; }

    [BsonElement("ipAddress")]
    public string IpAddress { get; set; }

    [BsonElement("userAgent")]
    public string UserAgent { get; set; }

    [BsonElement("sessionToken")]
    public string SessionToken { get; set; }

    [BsonElement("telegramUserId")]
    [BsonRepresentation(BsonType.Int64)]
    public long? TelegramUserId { get; set; }

    [BsonElement("isLinkedWithTelegram")]
    public bool IsLinkedWithTelegram { get; set; }

    [BsonElement("utmSource")]
    public string UtmSource { get; set; }   

    [BsonElement("utmCampaign")]
    public string UtmCampaign { get; set; }

    [BsonElement("utmContent")]
    public string UtmContent { get; set; }

    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

