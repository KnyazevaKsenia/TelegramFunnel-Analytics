using ClosedXML.Excel;
using CommonRabbitMq;
using StatisticLibrary.Models.StatisticModels;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Implementations;

public class ExcelGenerator : IExcelGenerator
{
    private readonly ILogger<ExcelGenerator> _logger;
    private readonly IConfiguration _configuration;

    public ExcelGenerator(
        ILogger<ExcelGenerator> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task<ReportResult> GenerateExcelReportAsync(ReportTask task, ProjectStatistics statistics)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Генерация Excel отчета {ReportId}", task.ReportId);

            var excelBytes = GenerateExcelBytes(statistics);
    
            var fileName = $"report_{task.ReportId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            var storagePath = GetStoragePath();
            var fullPath = Path.Combine(storagePath, "excel", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, excelBytes);

            stopwatch.Stop();

            return new ReportResult
            {
                ReportId = task.ReportId,
                FileName = fileName,
                FileSize = excelBytes.Length,
                FileBytes = excelBytes, 
                GeneratedAt = DateTime.UtcNow,
                IsSuccess = true
            };

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка генерации Excel отчета {ReportId}", task.ReportId);

            return new ReportResult
            {
                ReportId = task.ReportId,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public byte[] GenerateExcelBytes(ProjectStatistics statistics)
    {
        using var workbook = new XLWorkbook();
        using var memoryStream = new MemoryStream();

        AddSummarySheet(workbook, statistics);

        if (statistics.DailyStats.Any())
            AddDailyStatsSheet(workbook, statistics.DailyStats);
        
        if (statistics.SourceStats.Any())
            AddSourceStatsSheet(workbook, statistics.SourceStats);

        if (statistics.CampaignStats.Any())
            AddCampaignStatsSheet(workbook, statistics.CampaignStats);
        
        if (statistics.ContentStats.Any())
            AddContentStatsSheet(workbook, statistics.ContentStats);

        if (statistics.DeviceStats.Any())
            AddDeviceStatsSheet(workbook, statistics.DeviceStats);

        if (statistics.LocationStats.Any())
            AddLocationStatsSheet(workbook, statistics.LocationStats);

        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }


    private void AddSummarySheet(IXLWorkbook workbook, ProjectStatistics statistics)
    {
        var ws = workbook.Worksheets.Add("Сводка");
        
        ws.Cell(1, 1).Value = "Telegram Funnel Analytics — Сводный отчет";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, 4).Merge();
        
        ws.Cell(2, 1).Value = "Период:";
        ws.Cell(2, 2).Value =
            $"{statistics.PeriodStart:dd.MM.yyyy} - {statistics.PeriodEnd:dd.MM.yyyy}";

        ws.Cell(4, 1).Value = "Ключевые метрики";
        ws.Cell(4, 1).Style.Font.Bold = true;
        ws.Range(4, 1, 4, 4).Merge();
        
        int clicks = statistics.TotalClicks;
        int subs = statistics.TotalSubscriptions;
        int users = statistics.UniqueUsers;
        double conv = statistics.ConversionRate;
        
        ws.Cell(5, 1).Value = "Всего кликов";
        ws.Cell(5, 2).Value = clicks;

        ws.Cell(6, 1).Value = "Всего подписок";
        ws.Cell(6, 2).Value = subs;

        ws.Cell(7, 1).Value = "Конверсия";
        ws.Cell(7, 2).Value = conv;
        ws.Cell(7, 2).Style.NumberFormat.Format = "0.00\"%\"";
        
        ws.Cell(8, 1).Value = "Уникальные пользователи";
        ws.Cell(8, 2).Value = users;

        ws.Columns().AdjustToContents();
    }

    private void AddDailyStatsSheet(IXLWorkbook workbook, List<DailyStat> dailyStats)
    {
        var ws = workbook.Worksheets.Add("По дням");

        ws.Cell(1, 1).Value = "Статистика по дням";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "Дата", "Клики", "Подписки", "Конверсия", "Тренд" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
        }

        var row = 3;
        foreach (var s in dailyStats.OrderBy(d => d.Date))
        {
            ws.Cell(row, 1).Value = s.Date;
            ws.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy";
            
            ws.Cell(row, 2).Value = s.Clicks;
            ws.Cell(row, 3).Value = s.Subscriptions;

            ws.Cell(row, 4).Value = s.ConversionRate ;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.00\"%\"";
            

            row++;
        }

        ws.Cell(row, 1).Value = "ИТОГО:";
        ws.Cell(row, 1).Style.Font.Bold = true;

        ws.Cell(row, 2).FormulaA1 = $"SUM(B3:B{row - 1})";
        ws.Cell(row, 3).FormulaA1 = $"SUM(C3:C{row - 1})";

        ws.Columns().AdjustToContents();
    }

    private void AddSourceStatsSheet(IXLWorkbook workbook, List<SourceStat> sourceStats)
    {
        var ws = workbook.Worksheets.Add("Источники");

        ws.Cell(1, 1).Value = "Статистика по источникам трафика";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 4).Merge();

        var headers = new[] { "Источник", "Клики", "Подписки", "Конверсия" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
        }

        var row = 3;
        foreach (var s in sourceStats.OrderByDescending(s => s.Clicks))
        {
            ws.Cell(row, 1).Value = s.Source ?? "Не указан";
            ws.Cell(row, 2).Value = s.Clicks;
            ws.Cell(row, 3).Value = s.Subscriptions;

            ws.Cell(row, 4).Value = s.ConversionRate;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.00\"%\"";

            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void AddCampaignStatsSheet(IXLWorkbook workbook, List<CampaignStat> stats)
    {
        var ws = workbook.Worksheets.Add("Кампании");

        ws.Cell(1, 1).Value = "Статистика по рекламным кампаниям";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 4).Merge();

        var headers = new[] { "Кампания", "Клики", "Подписки", "Конверсия" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
        }

        var row = 3;
        foreach (var s in stats.OrderByDescending(c => c.Clicks))
        {
            ws.Cell(row, 1).Value = s.Campaign ?? "Не указана";
            ws.Cell(row, 2).Value = s.Clicks;
            ws.Cell(row, 3).Value = s.Subscriptions;

            ws.Cell(row, 4).Value = s.ConversionRate ;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.00\"%\"";

            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void AddContentStatsSheet(IXLWorkbook workbook, List<ContentStat> stats)
    {
        var ws = workbook.Worksheets.Add("Контент");

        ws.Cell(1, 1).Value = "Статистика по контенту";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 4).Merge();

        var headers = new[] { "Контент", "Клики", "Подписки", "Конверсия" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
        }

        var row = 3;
        foreach (var s in stats.OrderByDescending(c => c.Clicks))
        {
            ws.Cell(row, 1).Value = s.Content ?? "Не указан";
            ws.Cell(row, 2).Value = s.Clicks;
            ws.Cell(row, 3).Value = s.Subscriptions;

            ws.Cell(row, 4).Value = s.ConversionRate ;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.00\"%\"";

            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void AddDeviceStatsSheet(IXLWorkbook workbook, List<DeviceStat> stats)
    {
        var ws = workbook.Worksheets.Add("Устройства");

        ws.Cell(1, 1).Value = "Статистика по устройствам и браузерам";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "Тип устройства", "Браузер", "Клики", "Подписки", "Конверсия" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
        }

        var row = 3;
        foreach (var s in stats.OrderByDescending(d => d.Clicks))
        {
            ws.Cell(row, 1).Value = s.DeviceType ?? "Не определен";
            ws.Cell(row, 2).Value = s.Browser ?? "Не определен";
            ws.Cell(row, 3).Value = s.Clicks;
            ws.Cell(row, 4).Value = s.Subscriptions;

            ws.Cell(row, 5).Value = s.ConversionRate ;
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.00\"%\"";

            row++;
        }

        ws.Columns().AdjustToContents();
    }
    private void AddLocationStatsSheet(IXLWorkbook workbook, List<LocationStat> stats)
    {
        var ws = workbook.Worksheets.Add("Локации");

        ws.Cell(1, 1).Value = "Статистика по географии";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "Страна", "Город", "Клики", "Подписки", "Конверсия" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
        }

        var row = 3;
        foreach (var s in stats.OrderByDescending(l => l.Clicks))
        {
            ws.Cell(row, 1).Value = s.Country ?? "Не определена";
            ws.Cell(row, 2).Value = s.City ?? "Не определен";
            ws.Cell(row, 3).Value = s.Clicks;
            ws.Cell(row, 4).Value = s.Subscriptions;

            ws.Cell(row, 5).Value = s.ConversionRate ;
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.00\"%\"";

            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private string GetStoragePath()
    {
        var configured = _configuration.GetValue<string>("ReportSettings:StoragePath");

        if (!string.IsNullOrEmpty(configured))
            return configured;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(appData, "TelegramFunnelAnalytics", "Reports");
    }
}

