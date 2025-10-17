
namespace Telegram_Analytic.Models.MongoDb;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ClickEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("linkId")]
    public Guid LinkId { get; set; }

    [BsonElement("projectId")]
    public Guid ProjectId { get; set; }

    [BsonElement("ipAddress")]
    public string IpAddress { get; set; }

    [BsonElement("userAgent")]
    public string UserAgent { get; set; }

    [BsonElement("referrer")]
    public string Referrer { get; set; }

    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("country")]
    public string Country { get; set; }

    [BsonElement("city")]
    public string City { get; set; }

    [BsonElement("deviceType")]
    public string DeviceType { get; set; }

    [BsonElement("browser")]
    public string Browser { get; set; }

    [BsonElement("platform")]
    public string Platform { get; set; }

    [BsonElement("clickToken")]
    public string ClickToken { get; set; } // Для связи с Telegram ботом
}
