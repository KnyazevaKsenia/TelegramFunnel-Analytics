using Microsoft.AspNetCore.Mvc;
using StatisticLibrary.Interfaces;
using StatisticLibrary.Models.StatisticModels;
namespace Telegram_Analytic.Controllers;

[ApiController]
[Route("api/projects/{projectId}/stats")]
public class ProjectStatsController : ControllerBase
{
    private readonly IProjectStatisticManager _statsManager;
    
    public ProjectStatsController(IProjectStatisticManager statsManager)
    {
        _statsManager = statsManager;
    }
    
    [HttpGet]
    public async Task<ActionResult<ProjectStatistics>> GetStats(
        Guid projectId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string[] sources,
        [FromQuery] string[] campaigns,
        [FromQuery] string[] contents) 
    {
        var filter = new StatFilter
        {
            ProjectId = projectId,
            StartDate = startDate,
            EndDate = endDate,
            Sources = sources,
            Campaigns = campaigns,
            Contents = contents 
        };
    
        var stats = await _statsManager.GetProjectStatsAsync(filter);
        return Ok(stats);
    }
    
    [HttpGet("charts/{chartType}")]
    public async Task<ActionResult<object>> GetChartData(
        Guid projectId,
        string chartType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new StatFilter
        {
            ProjectId = projectId,
            StartDate = startDate,
            EndDate = endDate
        };
    
        var chartData = await _statsManager.GetChartDataAsync(filter, chartType);
        return Ok(chartData);
    }
}

