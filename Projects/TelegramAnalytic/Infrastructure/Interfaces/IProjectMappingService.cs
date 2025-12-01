using Telegram_Analytic.Models;
using Telegram_Analytic.Models.ProjectModels;

namespace Telegram_Analytic.Infrastructure.Interfaces;

public interface IProjectMappingService
{
    public Task<ProjectViewModel> MapProjectWithStatsAsync(Project project);
    
}