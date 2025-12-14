using System.ComponentModel.DataAnnotations;

namespace CommonRabbitMq;

public class ReportTask
{
    [Required]
    public Guid ReportId { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    public required string Email { get; set; }
}
