using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram_Analytic.Infrastructure.Database;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models;
using Telegram_Analytic.Models.ProjectModels;
namespace Telegram_Analytic.Controllers;

public class ProjectController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITrackingLinksService _linkService;
    private const int MaxLinksPerProject = 20;
    private readonly ITgBotService _tgBotService;
    private readonly IProjectMappingService _projectMappingService;
    private readonly IMongoClickService _mongoClickService;
    public ProjectController(ApplicationDbContext context, ITrackingLinksService linkService, ITgBotService tgBotService, 
                                                                            IProjectMappingService projectMappingService,
                                                                                IMongoClickService mongoClickService)
    {
        _context = context;
        _tgBotService = tgBotService;
        _linkService = linkService;
        _projectMappingService = projectMappingService;
        _mongoClickService = mongoClickService;
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
            
            var projectModel = await _projectMappingService.MapProjectWithStatsAsync(project);
            
            var isBotAdmin = false;
            if (!string.IsNullOrEmpty(project.TelegramChanelUrl))
            {
                isBotAdmin = await _tgBotService.IsBotAdmin(project.TelegramChanelUrl);
            }
            
            
            ViewBag.MaxLinksPerProject = MaxLinksPerProject;
            ViewBag.CurrentLinksCount = project.TrackingLinks.Count;
            ViewBag.IsBotAdmin = isBotAdmin;
            ViewBag.TelegramChanelUrl = project.TelegramChanelUrl;
            
            return View("ProjectDetails", projectModel);
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

            if (project.TrackingLinks.Count >= MaxLinksPerProject)
            {
                return Json(new
                {
                    success = false,
                    error = $"Превышено максимальное количество ссылок ({MaxLinksPerProject}) для проекта"
                });
            }

            var link = await _linkService.CreateTrackingLink(
                request.Name, projectId,
                request.UtmContent, request.UtmCampaign, request.UtmSource);

            await _context.TrackingLinks.AddAsync(link);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                link = new
                {
                    id = link.Id,
                    name = link.Name,
                    generatedUrl = link.GeneratedUrl,
                    createdAt = link.CreatedAt,
                    isActive = link.IsActive
                },
                currentCount = project.TrackingLinks.Count + 1,
                maxCount = MaxLinksPerProject
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
            await _mongoClickService.DeleteLinkClicks(linkId);
            
            
            var project = await _context.Projects
                .Include(p => p.TrackingLinks)
                .FirstOrDefaultAsync(p => p.Id == link.ProjectId);
            
            return Json(new { 
                success = true,
                currentCount = project?.TrackingLinks.Count ?? 0,
                maxCount = MaxLinksPerProject
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}

