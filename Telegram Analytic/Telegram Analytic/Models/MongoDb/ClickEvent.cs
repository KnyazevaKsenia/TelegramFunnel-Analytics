using MongoDB.Bson;

namespace Telegram_Analytic.Models.MongoDb;

public class ClickEvent
{
    public ObjectId Id { get; set; }
    public Guid LinkId { get; set; }
    public Guid ProjectId { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string Referrer { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
