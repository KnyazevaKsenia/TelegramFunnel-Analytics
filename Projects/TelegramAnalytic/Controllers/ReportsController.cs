using CommonRabbitMq;
using Microsoft.AspNetCore.Mvc;
using Telegram_Analytic.Models;
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
            Console.WriteLine(reportRequest.EndDate);
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
    
    [HttpPost]
    public async Task<IActionResult> RequestAiAnalysis([FromBody] CreateAiAnalysisRequest request)
    {
        try
        {
            if (request.ProjectId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "ProjectId не указан"
                });
            }

            if (request.StartDate > request.EndDate)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Дата начала не может быть позже даты окончания"
                });
            }

            var analysisId = await _rabbitMqService.CreateAiAnalysisAsync(request);

            if (analysisId == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Не удалось создать AI-анализ"
                });
            }

            return Ok(new
            {
                success = true,
                analysisId,
                message = "AI-анализ запущен"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error"
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAiAnalysis(Guid analysisId)
    {
        var result = await _rabbitMqService.GetAiAnalysisAsync(analysisId);

        if (result == null)
        {
            return NotFound(new
            {
                success = false,
                message = "AI-анализ не найден"
            });
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectAiAnalysisStatuses(Guid projectId)
    {
        var result = await _rabbitMqService.GetProjectAiAnalysisStatusesAsync(projectId);
        return Ok(result);
    }
}