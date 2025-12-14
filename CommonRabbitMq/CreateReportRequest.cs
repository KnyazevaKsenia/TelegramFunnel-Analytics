using System.ComponentModel.DataAnnotations;

namespace CommonRabbitMq;

public class CreateReportRequest
{
    public required string ProjectId { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public required string Email { get; set; }
    
    public required string Format { get; set; }
}
