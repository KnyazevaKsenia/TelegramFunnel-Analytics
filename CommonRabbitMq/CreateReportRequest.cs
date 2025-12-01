using System.ComponentModel.DataAnnotations;

namespace CommonRabbitMq;

public class CreateReportRequest
{
    [Required]
    public string ProjectId { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Format { get; set; }
}
