using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonMongoModels;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ReportStatus
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    [BsonRepresentation(BsonType.String)] 
    public Guid ReportId { get; set; }
    
    [BsonRepresentation(BsonType.String)] 
    public Guid ProjectId { get; set; }
    
    public string Format { get; set; }
    public string Status { get; set; }
    public string? ErrorMessage { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
}



