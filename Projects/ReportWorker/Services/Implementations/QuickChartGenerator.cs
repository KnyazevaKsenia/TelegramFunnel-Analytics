using System.Text;
using System.Text.Json;
using SkiaSharp;
using StatisticLibrary.Models.StatisticModels;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Implementations;

public class QuickChartGenerator : IChartGenerator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuickChartGenerator> _logger;

    public QuickChartGenerator(
        HttpClient httpClient,
        ILogger<QuickChartGenerator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<byte[]> GenerateDailyChartAsync(ProjectStatistics stats)
    {
        if (!stats.DailyStats.Any())
            return Task.FromResult(CreateFallbackChart("Клики и подписки по дням", "Нет данных"));

        var ordered = stats.DailyStats
            .OrderBy(x => x.Date)
            .ToList();

        var labels = ordered
            .Select(x => x.Date.ToString("dd.MM"))
            .ToArray();

        var clicks = ordered
            .Select(x => x.Clicks)
            .ToArray();

        var subscriptions = ordered
            .Select(x => x.Subscriptions)
            .ToArray();

        var chart = new
        {
            type = "line",
            data = new
            {
                labels,
                datasets = new object[]
                {
                    new
                    {
                        label = "Клики",
                        data = clicks,
                        borderColor = "#2563EB",
                        backgroundColor = "rgba(37, 99, 235, 0.12)",
                        fill = true,
                        tension = 0.35,
                        pointRadius = 4,
                        pointHoverRadius = 5,
                        borderWidth = 3
                    },
                    new
                    {
                        label = "Подписки",
                        data = subscriptions,
                        borderColor = "#10B981",
                        backgroundColor = "rgba(16, 185, 129, 0.12)",
                        fill = true,
                        tension = 0.35,
                        pointRadius = 4,
                        pointHoverRadius = 5,
                        borderWidth = 3
                    }
                }
            },
            options = new
            {
                responsive = true,
                plugins = new
                {
                    legend = new
                    {
                        position = "bottom",
                        labels = new
                        {
                            font = new
                            {
                                size = 14
                            },
                            usePointStyle = true
                        }
                    },
                    title = new
                    {
                        display = true,
                        text = "Клики и подписки по дням",
                        color = "#0F172A",
                        font = new
                        {
                            size = 22,
                            weight = "bold"
                        }
                    }
                },
                scales = new
                {
                    y = new
                    {
                        beginAtZero = true,
                        ticks = new
                        {
                            color = "#64748B"
                        },
                        grid = new
                        {
                            color = "rgba(148, 163, 184, 0.25)"
                        }
                    },
                    x = new
                    {
                        ticks = new
                        {
                            color = "#64748B"
                        },
                        grid = new
                        {
                            display = false
                        }
                    }
                }
            }
        };

        return RenderChartAsync(chart, "Клики и подписки по дням");
    }

    public Task<byte[]> GenerateSourcesChartAsync(ProjectStatistics stats)
    {
        var data = stats.SourceStats
            .OrderByDescending(x => x.Clicks)
            .Take(8)
            .Select(x => new ChartItem(x.Source ?? "Не указан", x.Clicks))
            .ToList();

        return GenerateHorizontalBarChartAsync(
            "Источники трафика",
            data,
            "#2563EB");
    }

    public Task<byte[]> GenerateCampaignsChartAsync(ProjectStatistics stats)
    {
        var data = stats.CampaignStats
            .OrderByDescending(x => x.Clicks)
            .Take(8)
            .Select(x => new ChartItem(x.Campaign ?? "Не указана", x.Clicks))
            .ToList();

        return GenerateHorizontalBarChartAsync(
            "Кампании",
            data,
            "#7C3AED");
    }

    public Task<byte[]> GenerateLocationsChartAsync(ProjectStatistics stats)
    {
        var data = stats.LocationStats
            .OrderByDescending(x => x.Clicks)
            .Take(8)
            .Select(x => new ChartItem(
                $"{x.Country ?? "Не определена"} / {x.City ?? "Не определен"}",
                x.Clicks))
            .ToList();

        return GenerateHorizontalBarChartAsync(
            "География",
            data,
            "#10B981");
    }

    public Task<byte[]> GenerateDevicesChartAsync(ProjectStatistics stats)
    {
        var data = stats.DeviceStats
            .OrderByDescending(x => x.Clicks)
            .Take(8)
            .Select(x => new ChartItem(
                $"{x.DeviceType ?? "Не определено"} ({x.Browser ?? "Не определен"})",
                x.Clicks))
            .ToList();

        return GenerateHorizontalBarChartAsync(
            "Устройства и браузеры",
            data,
            "#F59E0B");
    }

    public Task<byte[]> GenerateContentChartAsync(ProjectStatistics stats)
    {
        var data = stats.ContentStats
            .OrderByDescending(x => x.Clicks)
            .Take(8)
            .Select(x => new ChartItem(x.Content ?? "Не указан", x.Clicks))
            .ToList();

        return GenerateHorizontalBarChartAsync(
            "Контент",
            data,
            "#EF4444");
    }

    private Task<byte[]> GenerateHorizontalBarChartAsync(
        string title,
        List<ChartItem> items,
        string color)
    {
        if (!items.Any())
            return Task.FromResult(CreateFallbackChart(title, "Нет данных"));

        var labels = items.Select(x => TrimLabel(x.Label, 32)).ToArray();
        var values = items.Select(x => x.Value).ToArray();

        var chart = new
        {
            type = "bar",
            data = new
            {
                labels,
                datasets = new object[]
                {
                    new
                    {
                        label = "Клики",
                        data = values,
                        backgroundColor = color,
                        borderColor = color,
                        borderWidth = 1,
                        borderRadius = 8,
                        barThickness = 28
                    }
                }
            },
            options = new
            {
                indexAxis = "y",
                responsive = true,
                plugins = new
                {
                    legend = new
                    {
                        display = false
                    },
                    title = new
                    {
                        display = true,
                        text = title,
                        color = "#0F172A",
                        font = new
                        {
                            size = 22,
                            weight = "bold"
                        }
                    }
                },
                scales = new
                {
                    x = new
                    {
                        beginAtZero = true,
                        ticks = new
                        {
                            color = "#64748B"
                        },
                        grid = new
                        {
                            color = "rgba(148, 163, 184, 0.25)"
                        }
                    },
                    y = new
                    {
                        ticks = new
                        {
                            color = "#334155",
                            font = new
                            {
                                size = 13
                            }
                        },
                        grid = new
                        {
                            display = false
                        }
                    }
                }
            }
        };

        return RenderChartAsync(chart, title);
    }

    private async Task<byte[]> RenderChartAsync(object chart, string fallbackTitle)
    {
        try
        {
            var request = new
            {
                version = "4",
                backgroundColor = "white",
                width = 900,
                height = 430,
                devicePixelRatio = 2,
                format = "png",
                chart
            };

            var json = JsonSerializer.Serialize(request);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(
                "https://quickchart.io/chart",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                _logger.LogError(
                    "Ошибка QuickChart. StatusCode: {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    error);

                return CreateFallbackChart(fallbackTitle, "Ошибка генерации графика");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запросе к QuickChart");
            return CreateFallbackChart(fallbackTitle, "Ошибка генерации графика");
        }
    }

    private static string TrimLabel(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Не указано";

        return value.Length <= maxLength
            ? value
            : value[..maxLength] + "…";
    }

    private static byte[] CreateFallbackChart(string title, string message)
    {
        using var bitmap = new SKBitmap(900, 430);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.White);

        using var titlePaint = new SKPaint
        {
            Color = new SKColor(15, 23, 42),
            TextSize = 28,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        using var textPaint = new SKPaint
        {
            Color = new SKColor(100, 116, 139),
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        canvas.DrawText(title, 40, 70, titlePaint);
        canvas.DrawText(message, 40, 120, textPaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private sealed class ChartItem
    {
        public ChartItem(string label, int value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public int Value { get; }
    }
}