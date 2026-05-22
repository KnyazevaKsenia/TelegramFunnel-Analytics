using CommonRabbitMq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CommonMongoModels;

public class AiAnalysisStatus
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid AnalysisId { get; set; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid ProjectId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string Status { get; set; } = "В очереди";
    
    public AiReportContent? Content { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}