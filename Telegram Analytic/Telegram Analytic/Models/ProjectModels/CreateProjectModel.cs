using System.ComponentModel.DataAnnotations;

namespace Telegram_Analytic.Models.ProjectModels;

public class CreateProjectModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Display(Name = "Токен бота Telegram")]
    public string TelegramBotToken { get; set; }

    [Display(Name = "ID канала Telegram")]
    public string TelegramChatId { get; set; }
}

