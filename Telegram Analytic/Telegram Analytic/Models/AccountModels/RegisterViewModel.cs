namespace Telegram_Analytic.Models;

// Models/ViewModels/RegisterViewModel.cs
using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Имя обязательно")]
    [Display(Name = "Имя")]
    [StringLength(50, ErrorMessage = "Имя не может превышать 50 символов")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Фамилия обязательна")]
    [Display(Name = "Фамилия")]
    [StringLength(50, ErrorMessage = "Фамилия не может превышать 50 символов")]
    public string LastName { get; set; }

    [Display(Name = "Компания/Бренд")]
    [StringLength(100, ErrorMessage = "Название компании не может превышать 100 символов")]
    public string Company { get; set; }

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат Email")]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, ErrorMessage = "Пароль должен содержать от {2} до {1} символов", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Необходимо принять условия использования")]
    [Display(Name = "Я принимаю условия использования и политику конфиденциальности")]
    public bool AcceptTerms { get; set; }
}