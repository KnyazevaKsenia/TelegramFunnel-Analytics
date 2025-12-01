using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonMongoModels;

public class ReportStatus
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public Guid ReportId { get; set; }
    public Guid ProjectId { get; set; }
    public string Format { get; set; }
    public string Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}



