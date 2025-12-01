using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonMongoModels;

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

    [BsonElement("telegramUserName")]
    public string? TelegramUserName { get; set; }

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
    public long User_Id {get; set;}
    public bool IsSubscribed { get; set; }
}

