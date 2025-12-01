using AutoMapper;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models;
using Telegram_Analytic.Models.ProjectModels;

namespace Telegram_Analytic.Services;

public class ProjectMappingService : IProjectMappingService
{
    private readonly IMapper _mapper;
    private readonly IMongoClickService _mongoClickService;

    public ProjectMappingService(IMapper mapper, IMongoClickService mongoClickService)
    {
        _mapper = mapper;
        _mongoClickService = mongoClickService;
    }
    
    public async Task<ProjectViewModel> MapProjectWithStatsAsync(Project project)
    {
        var viewModel = _mapper.Map<ProjectViewModel>(project);
        viewModel.TrackingLinks = _mapper.Map<List<TrackingLinkViewModel>>(project.TrackingLinks);
        
        var linkIds = project.TrackingLinks.Select(tl => tl.Id).ToList();
        var clickCounts = await _mongoClickService.GetClickCountsForLinksAsync(linkIds);
        
        foreach (var trackingLink in viewModel.TrackingLinks)
        {
            trackingLink.ClickCount = clickCounts.GetValueOrDefault(trackingLink.Id, 0L);
        }
        
        return viewModel;
    }
}