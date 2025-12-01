

using StatisticLibrary.Models.StatisticModels;


namespace StatisticLibrary.Interfaces;

public interface IProjectStatisticManager
{
    Task<ProjectStatistics> GetProjectStatsAsync(StatFilter filter);
    Task<object> GetChartDataAsync(StatFilter filter, string chartType);
}