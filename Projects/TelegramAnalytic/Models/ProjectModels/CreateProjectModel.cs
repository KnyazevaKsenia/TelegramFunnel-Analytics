using System.ComponentModel.DataAnnotations;

namespace Telegram_Analytic.Models.ProjectModels;

public class CreateProjectModel
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    
    [Display(Name = "Ссылка на телеграм-канал")]
    [RegularExpression(@"^(https?:\/\/t\.me\/[a-zA-Z0-9_]{5,32}|@[a-zA-Z0-9_]{5,32}|[a-zA-Z0-9_]{5,32})$", 
        ErrorMessage = "Введите корректную ссылку Telegram (https://t.me/username)")]
    public required string TelegramChanelUrl { get; set; }
}

