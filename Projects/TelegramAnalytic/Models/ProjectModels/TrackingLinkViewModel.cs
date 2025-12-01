using System.ComponentModel.DataAnnotations;

namespace Telegram_Analytic.Models.ProjectModels;

public class TrackingLinkViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Название ссылки")]
    public string Name { get; set; }
    
    [Required]
    [Url]
    [Display(Name = "Целевой URL")]
    public string BaseUrl { get; set; }
    
    [Display(Name = "Идентификатор ссылки")]
    public string UrlIdentifier { get; set; }

    [Display(Name = "Сгенерированный URL")]
    public string GeneratedUrl { get; set; }
    
    // UTM-параметры
    public string UtmSource { get; set; }
    public string UtmCampaign { get; set; }
    public string UtmContent { get; set; }

    [Display(Name = "Активна")]
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastClickedAt { get; set; }
    public long ClickCount { get; set; }
}