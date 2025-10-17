namespace Telegram_Analytic.Models;


using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    [Required]
    [Display(Name = "Имя")]
    [StringLength(50, ErrorMessage = "Имя не может превышать 50 символов")]
    public string FirstName { get; set; }

    [Required]
    [Display(Name = "Фамилия")]
    [StringLength(50, ErrorMessage = "Фамилия не может превышать 50 символов")]
    public string LastName { get; set; }

    [Display(Name = "Компания/Бренд")]
    [StringLength(100, ErrorMessage = "Название компании не может превышать 100 символов")]
    public string Company { get; set; }
        
    [Display(Name = "Дата регистрации")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Последний вход")]
    public DateTime? LastLoginAt { get; set; }

    [Display(Name = "Аккаунт активирован")]
    public bool IsActive { get; set; } = true;

    // Навигационные свойства
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}


public static class ApplicationUserExtensions
{
    public static string GetFullName(this ApplicationUser user)
    {
        return $"{user.FirstName} {user.LastName}";
    }
}