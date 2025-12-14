using CommonRabbitMq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StatisticLibrary.Models.StatisticModels;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Implementations
{
    public class PdfGenerator : IPdfGenerator
    {
        private readonly IChartGenerator _charts;

        public PdfGenerator(IChartGenerator charts)
        {
            _charts = charts;
        }
        public async System.Threading.Tasks.Task<ReportResult> GeneratePdfReportAsync(ReportTask task, ProjectStatistics stats)
        {
            var result = new ReportResult
            {
                ReportId = Guid.NewGuid(),
                GeneratedAt = DateTime.UtcNow
            };
            
            try
            {
                result.FileName =
                    $"report_{task.ProjectId}_{task.StartDate:yyyyMMdd}_{task.EndDate:yyyyMMdd}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

                result.FileBytes = await GeneratePdfBytes(task, stats);
                result.FileSize = result.FileBytes.Length;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Ошибка генерации отчёта: {ex.Message}";
                result.FileBytes = null;
                result.FileSize = 0;
            }

            return result;
        }
        
        public async Task<byte[]> GeneratePdfBytes(ReportTask task, ProjectStatistics stats)
        {
            var dailyChart = _charts.GenerateDailyChart(stats);
            var sourcesChart = _charts.GenerateSourcesChart(stats);
            var devicesChart = _charts.GenerateDevicesChart(stats);
            var locationsChart = _charts.GenerateLocationsChart(stats);
            var campaignsChart = _charts.GenerateCampaignsChart(stats);
            var contentChart = _charts.GenerateContentChart(stats);

            var pdfBytes = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Margin(60);
                    page.Size(PageSizes.A4);

                    page.Content().Column(col =>
                    {
                        col.Spacing(20);

                        col.Item().AlignCenter().Text("Telegram Funnel Analytics")
                            .FontSize(36)
                            .Bold()
                            .FontColor("#2563EB"); 

                        col.Item().AlignCenter().Text("Аналитический отчёт")
                            .FontSize(18)
                            .Light();

                        col.Item().AlignCenter()
                            .Text($"Период: {task.StartDate:dd.MM.yyyy} — {task.EndDate:dd.MM.yyyy}")
                            .FontSize(14)
                            .FontColor("#555");

                        col.Item().PaddingVertical(40);

                        col.Item()
                            .Border(1)
                            .BorderColor("#E5E7EB")
                            .CornerRadius(16)
                            .Padding(25)
                            .Column(inner =>
                            {
                                inner.Item().AlignCenter()
                                    .Text("СОДЕРЖАНИЕ")
                                    .FontSize(22)
                                    .Bold();

                                inner.Item().PaddingTop(20);

                                inner.Item().Text("1. Общая статистика").FontSize(16);
                                inner.Item().Text("2. Динамика по дням").FontSize(16);
                                inner.Item().Text("3. Источники трафика").FontSize(16);
                                inner.Item().Text("4. Устройства и география").FontSize(16);
                                inner.Item().Text("5. Кампании").FontSize(16);
                                inner.Item().Text("6. Контент").FontSize(16);
                            });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница "); x.CurrentPageNumber(); x.Span(" из "); x.TotalPages();
                    });
                });

                document.Page(page =>
                {
                    page.Margin(40);

                    page.Content().Column(col =>
                    {
                        // Метрики друг под другом
                        col.Item().Element(ComposeSummary(stats));

                        col.Item().PaddingTop(15).Element(ComposeChartBlock("Клики и подписки по дням", dailyChart));

                        col.Item().PaddingTop(15).Element(ComposeCardWithChartAndTable(
                            "Источники трафика",
                            sourcesChart,
                            stats.SourceStats.Select(s => new TableRow
                            {
                                Name = s.Source,
                                Clicks = s.Clicks,
                                Subscriptions = s.Subscriptions,
                                Conversion = s.ConversionRate
                            }).ToList()
                        ));
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница "); x.CurrentPageNumber(); x.Span(" из "); x.TotalPages();
                    });
                });

                document.Page(page =>
                {
                    page.Margin(40);
                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(5).Element(ComposeCardWithChartAndTable(
                            "Устройства и браузеры",
                            devicesChart,
                            stats.DeviceStats.Select(s => new TableRow
                            {
                                Name = $"{s.DeviceType} ({s.Browser})",
                                Clicks = s.Clicks,
                                Subscriptions = s.Subscriptions,
                                Conversion = s.ConversionRate
                            }).ToList()
                        ));

                        col.Item().PaddingTop(15).Element(
                            ComposeCardWithChartAndTable(
                                "География пользователей",
                                locationsChart,
                                stats.LocationStats.Select(l => new TableRow
                                {
                                    Name = $"{l.Country} / {l.City}",
                                    Clicks = l.Clicks,
                                    Subscriptions = l.Subscriptions,
                                    Conversion = l.ConversionRate
                                }).ToList()
                            )
                        );


                        col.Item().PaddingTop(15).Element(ComposeCardWithChartAndTable(
                            "Эффективность кампаний",
                            campaignsChart,
                            stats.CampaignStats.Select(c => new TableRow
                            {
                                Name = c.Campaign,
                                Clicks = c.Clicks,
                                Subscriptions = c.Subscriptions,
                                Conversion = c.ConversionRate
                            }).ToList()
                        ));
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница "); x.CurrentPageNumber(); x.Span(" из "); x.TotalPages();
                    });
                });
                
                document.Page(page =>
                {
                    page.Margin(40);
                    
                    page.Content().Column(col =>
                    {
                        col.Item().Element(
                            ComposeCardWithChartAndTable(
                                "Контент",
                                contentChart,
                                stats.ContentStats.Select(c => new TableRow
                                {
                                    Name = c.Content,
                                    Clicks = c.Clicks,
                                    Subscriptions = c.Subscriptions,
                                    Conversion = c.ConversionRate
                                }).ToList()
                            )
                        );

                    });
                    
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница "); x.CurrentPageNumber(); x.Span(" из "); x.TotalPages();
                    });
                });

            }).GeneratePdf();

            return await Task.FromResult(pdfBytes);
        }

        private Action<IContainer> ComposeSummary(ProjectStatistics stats)
        {
            return container =>
            {
                container.Column(col =>
                {
                    col.Item().Text("Общая статистика").Bold().FontSize(20);
                    col.Item().PaddingVertical(10);
                    
                    col.Item().Element(x => StatCard(x, "Клики", stats.TotalClicks.ToString(), "#FDECEF"));
                    col.Item().PaddingTop(5).Element(x => StatCard(x, "Подписки", stats.TotalSubscriptions.ToString(), "#E8F5FF"));
                    col.Item().PaddingTop(5).Element(x => StatCard(x, "Конверсия", $"{stats.ConversionRate}%", "#F9FBE7"));
                    col.Item().PaddingTop(5).Element(x => StatCard(x, "Уникальные", stats.UniqueUsers.ToString(), "#FFF3E0"));
                });
            };
        }

        private void StatCard(IContainer container, string title, string value, string color)
        {
            container.Border(1).BorderColor("#E5E7EB")
                     .Background(color)
                     .CornerRadius(12)
                     .Padding(15)
                     .Column(col =>
                     {
                         col.Item().Text(title).FontSize(12).Light().FontColor("#333");
                         col.Item().Text(value).FontSize(22).Bold().FontColor("#111");
                     });
        }
        
        private Action<IContainer> ComposeChartBlock(string title, byte[] chart) =>
            container =>
                container
                    .Border(1)
                    .BorderColor("#E5E7EB")
                    .CornerRadius(16)
                    .Padding(20)
                    .Column(col =>
                    {
                        if (!string.IsNullOrEmpty(title))
                            col.Item().Text(title).Bold().FontSize(18);

                        col.Item().Image(chart).FitWidth();
                    });
        
        private Action<IContainer> ComposeCardWithChartAndTable(
            string title,
            byte[] chart,
            List<TableRow> rows)
        {
            return container =>
                container
                    .Border(1)
                    .BorderColor("#E5E7EB")
                    .CornerRadius(16)
                    .Padding(20)
                    .Column(col =>
                    {
                        col.Spacing(10);

                        col.Item()
                            .Text(title)
                            .Bold()
                            .FontSize(18);

                        col.Item()
                            .Image(chart)
                            .FitWidth();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Название").Bold();
                                header.Cell().Text("Клики").Bold();
                                header.Cell().Text("Подписки").Bold();
                                header.Cell().Text("Конверсия").Bold();
                            });
                            
                            foreach (var r in rows)
                            {
                                table.Cell().Text(r.Name);
                                table.Cell().Text(r.Clicks.ToString());
                                table.Cell().Text(r.Subscriptions.ToString());
                                table.Cell().Text($"{(r.Conversion/100):P2}");
                            }
                        });
                    });
        }
        
    }

    public class TableRow
    {
        public string Name { get; set; }
        public int Clicks { get; set; }
        public int Subscriptions { get; set; }
        public double Conversion { get; set; }
    }
}

