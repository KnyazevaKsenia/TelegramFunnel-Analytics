using System.ComponentModel.DataAnnotations;

namespace Telegram_Analytic.Models.ProjectModels;

public class TrackingLinkViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Название ссылки")]
    public required string Name { get; set; }
    
    [Required]
    [Url]
    [Display(Name = "Целевой URL")]
    public required string BaseUrl { get; set; }
    
    [Display(Name = "Идентификатор ссылки")]
    public required string UrlIdentifier { get; set; }

    [Display(Name = "Сгенерированный URL")]
    public required string GeneratedUrl { get; set; }
    
    public required string UtmSource { get; set; }
    public required string UtmCampaign { get; set; }
    public required string UtmContent { get; set; }

    [Display(Name = "Активна")]
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastClickedAt { get; set; }
    public long ClickCount { get; set; }
}