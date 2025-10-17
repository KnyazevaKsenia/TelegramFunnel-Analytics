using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram_Analytic.Infrastructure.Database;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models.ProjectModels;
namespace Telegram_Analytic.Controllers;

public class ProjectController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITrackingLinksService _linkService;
    private const int MAX_LINKS_PER_PROJECT = 20;
    
    public ProjectController(ApplicationDbContext context, ITrackingLinksService linkService)
    {
        _context = context;
        _linkService = linkService;
    }
    
    public async Task<IActionResult> Index(string projectId)
    {
        if (!string.IsNullOrEmpty(projectId))
        {
            if (!Guid.TryParse(projectId, out var id))
            {
                return BadRequest("Неверный формат ID проекта");
            }
            
            var project = await _context.Projects
                .Include(p => p.TrackingLinks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }
            
            ViewBag.MaxLinksPerProject = MAX_LINKS_PER_PROJECT;
            ViewBag.CurrentLinksCount = project.TrackingLinks.Count;
            
            return View("ProjectDetails", project);
        }
        
        return View("Projects");
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateProjectTrackingLink([FromBody] CreateProjectTrackingLinkRequest request)
    {
    try
    {
        Console.WriteLine($"Received projectId: {request.ProjectId}");
        
        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            return Json(new { success = false, error = "Неверный формат ID проекта" });
        }
        
        var project = await _context.Projects
            .Include(p => p.TrackingLinks)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            return Json(new { success = false, error = "Проект не найден" });

        if (project.TrackingLinks.Count >= MAX_LINKS_PER_PROJECT)
        {
            return Json(new { 
                success = false, 
                error = $"Превышено максимальное количество ссылок ({MAX_LINKS_PER_PROJECT}) для проекта" 
            });
        }

        var link = _linkService.CreateTrackingLink(
            request.Name, request.BaseUrl, projectId,
            request.UtmContent, request.UtmCampaign, request.UtmSource);

        await _context.TrackingLinks.AddAsync(link);
        await _context.SaveChangesAsync();
        
        return Json(new { 
            success = true, 
            link = new {
                id = link.Id,
                name = link.Name,
                generatedUrl = link.GeneratedUrl,
                clickCount = link.ClickCount,
                createdAt = link.CreatedAt,
                isActive = link.IsActive
            },
            currentCount = project.TrackingLinks.Count + 1,
            maxCount = MAX_LINKS_PER_PROJECT
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, error = ex.Message });
    }
}



    
    [HttpPost]
    public async Task<IActionResult> DeleteTrackingLink(Guid linkId)
    {
        try
        {
            var link = await _context.TrackingLinks.FindAsync(linkId);
            if (link == null)
                return Json(new { success = false, error = "Ссылка не найдена" });

            _context.TrackingLinks.Remove(link);
            await _context.SaveChangesAsync();

            var project = await _context.Projects
                .Include(p => p.TrackingLinks)
                .FirstOrDefaultAsync(p => p.Id == link.ProjectId);

            return Json(new { 
                success = true,
                currentCount = project?.TrackingLinks.Count ?? 0,
                maxCount = MAX_LINKS_PER_PROJECT
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}