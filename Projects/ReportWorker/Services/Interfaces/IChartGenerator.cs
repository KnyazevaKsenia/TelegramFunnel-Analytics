using StatisticLibrary.Models.StatisticModels;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

public interface IChartGenerator
{
    byte[] GenerateDailyChart(ProjectStatistics stats);
    byte[] GenerateSourcesChart(ProjectStatistics stats);
    byte[] GenerateCampaignsChart(ProjectStatistics stats);
    byte[] GenerateLocationsChart(ProjectStatistics stats);
    byte[] GenerateDevicesChart(ProjectStatistics stats);
    byte[] GenerateContentChart(ProjectStatistics stats);
}
