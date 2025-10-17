using System.Security.Claims;
using Google.Rpc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Telegram_Analytic.Infrastructure.Database;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models;
using Telegram_Analytic.Models.ProjectModels;

namespace Telegram_Analytic.Controllers;

public class ProjectsController: Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ProjectsController> _logger;
    private readonly ApplicationDbContext _context;
    
    public ProjectsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        ILogger<ProjectsController> logger, ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _context = context;
    }
    
    public IActionResult Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }
        
        var userProjects = _context.Projects
            .Where(p => p.UserId == userId)
            .ToList();
    
        return View(userProjects);
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProjectModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        if (userId != null && ProjectNameExists(model.Name, userId))
        {
            ModelState.AddModelError("Name", "Проект с таким названием уже существует");
            return View(model);
        }
        
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }
            
            Project newProject = new Project
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                TelegramChatId = model.TelegramChatId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow, 
                UserId = userId
            };

            await _context.Projects.AddAsync(newProject);
            await _context.SaveChangesAsync(); 
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании проекта");
            ModelState.AddModelError("", "Произошла ошибка при создании проекта");
            return View(model);
        }
    }
    
    private bool ProjectNameExists(string name, string userId)
    {
        return _context.Projects.Any(p => p.Name == name && p.UserId == userId);
    }
}