using CommonRabbitMq;
using Microsoft.AspNetCore.Mvc;
using Telegram_Analytic.Models.ProjectModels;

namespace Telegram_Analytic.Controllers;

public class ReportsController : Controller
{
    private readonly IRabbitMqService _rabbitMqService;

    public ReportsController(IRabbitMqService rabbitMqService)
    {
        _rabbitMqService = rabbitMqService;
    }
    
    [HttpPost]
    public async Task<IActionResult> SendReportRequest([FromBody] CreateReportRequest reportRequest)
    {
        try
        {
            var result = await _rabbitMqService.CreateReportAsync(reportRequest);
            if (result)
            {
                return Ok(new { 
                    Success = true, 
                    Message = "Report generation started" 
                });
            }
            
            return BadRequest(new { Success = false });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, new { Success = false, Message = "Internal server error" });
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProjectReportsStatuses(Guid projectId)
    {
        var result = await _rabbitMqService.GetProjectReportStatusesAsync(projectId);
        if (result.Count > 0)
        {
            return Ok(result);
        }
        
        return Ok(new List<object>());
    }
}