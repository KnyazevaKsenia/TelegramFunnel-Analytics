using System.ComponentModel.DataAnnotations;

namespace Telegram_Analytic.Models.ProjectModels;

public class EditProjectModel
{
    public Guid Id { get; set; }
    
    [StringLength(100)]
    public string Name { get; set; } = "Мой проект";
}