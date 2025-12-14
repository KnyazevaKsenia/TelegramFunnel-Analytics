namespace Telegram_Analytic.Models;


using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат Email")]
    [Display(Name = "Email")]
    public required string Email { get; set; }
    
    [Required(ErrorMessage = "Пароль обязателен")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public required string Password { get; set; }
    
    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }
}