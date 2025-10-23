using System.ComponentModel.DataAnnotations;

namespace Telegram_Analytic.Models;

public class Project
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "Мой проект";
    
    [Display(Name = "Ссылка на телеграм-канал")]
    public string TelegramChanelUrl { get; set; }
    
    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    
    public virtual ICollection<TrackingLink> TrackingLinks { get; set; } = new List<TrackingLink>();
}