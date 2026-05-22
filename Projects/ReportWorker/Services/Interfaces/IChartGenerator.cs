using StatisticLibrary.Models.StatisticModels;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;


public interface IChartGenerator
{   
    Task<byte[]> GenerateDailyChartAsync(ProjectStatistics stats);
    Task<byte[]> GenerateSourcesChartAsync(ProjectStatistics stats);
    Task<byte[]> GenerateDevicesChartAsync(ProjectStatistics stats);
    Task<byte[]> GenerateLocationsChartAsync(ProjectStatistics stats);
    Task<byte[]> GenerateCampaignsChartAsync(ProjectStatistics stats);
    Task<byte[]> GenerateContentChartAsync(ProjectStatistics stats);
}