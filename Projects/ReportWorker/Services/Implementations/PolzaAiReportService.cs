using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;
using TelegramFunnelAnalytics.ReportWorker.Settings;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Implementations;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommonRabbitMq;
using Microsoft.Extensions.Options;
using StatisticLibrary.Models.StatisticModels;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;
using TelegramFunnelAnalytics.ReportWorker.Settings;

public class PolzaAiReportService : IAiReportService
{
    private readonly HttpClient _httpClient;
    private readonly PolzaAiSettings _settings;
    private readonly ILogger<PolzaAiReportService> _logger;

    public PolzaAiReportService(
        HttpClient httpClient,
        IOptions<PolzaAiSettings> settings,
        ILogger<PolzaAiReportService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AiReportContent> GenerateReportContentAsync(
        ReportTask task,
        ProjectStatistics statistics)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("PolzaAi:ApiKey не настроен. AI-анализ будет пропущен.");
                return GetFallbackContent(statistics);
            }

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var request = new
            {
                model = _settings.Model,
                temperature = 0.2,
                max_tokens = 1200,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = """
                        Ты аналитик Telegram Funnel Analytics.
                        Пиши на русском языке.
                        Не выдумывай данные, используй только переданные цифры.
                        Формулируй кратко, понятно и в деловом стиле.
                        """
                    },
                    new
                    {
                        role = "user",
                        content = BuildPrompt(task, statistics)
                    }
                },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "telegram_funnel_report_analysis",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                executiveSummary = new
                                {
                                    type = "string",
                                    description = "Краткое резюме отчета в 2-4 предложениях"
                                },
                                keyFindings = new
                                {
                                    type = "array",
                                    description = "Ключевые выводы по отчету",
                                    items = new { type = "string" }
                                },
                                recommendations = new
                                {
                                    type = "array",
                                    description = "Практические рекомендации по улучшению воронки",
                                    items = new { type = "string" }
                                },
                                risks = new
                                {
                                    type = "array",
                                    description = "Проблемы или риски, которые видны из статистики",
                                    items = new { type = "string" }
                                }
                            },
                            required = new[]
                            {
                                "executiveSummary",
                                "keyFindings",
                                "recommendations",
                                "risks"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Ошибка Polza.ai: {StatusCode}. Ответ: {Response}",
                    response.StatusCode,
                    responseText);

                return GetFallbackContent(statistics);
            }

            using var doc = JsonDocument.Parse(responseText);

            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                _logger.LogWarning("Polza.ai вернула пустой content.");
                return GetFallbackContent(statistics);
            }

            var aiContent = JsonSerializer.Deserialize<AiReportContent>(
                messageContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return aiContent ?? GetFallbackContent(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации AI-анализа отчета");
            return GetFallbackContent(statistics);
        }
    }

    private static string BuildPrompt(ReportTask task, ProjectStatistics statistics)
    {
        var dailyStats = statistics.DailyStats
            .OrderBy(x => x.Date)
            .Select(x => new
            {
                date = x.Date.ToString("dd.MM.yyyy"),
                clicks = x.Clicks,
                subscriptions = x.Subscriptions,
                conversionRate = x.ConversionRate
            });

        var sourceStats = statistics.SourceStats
            .OrderByDescending(x => x.Clicks)
            .Take(10)
            .Select(x => new
            {
                source = x.Source,
                clicks = x.Clicks,
                subscriptions = x.Subscriptions,
                conversionRate = x.ConversionRate
            });

        var campaignStats = statistics.CampaignStats
            .OrderByDescending(x => x.Clicks)
            .Take(10)
            .Select(x => new
            {
                campaign = x.Campaign,
                clicks = x.Clicks,
                subscriptions = x.Subscriptions,
                conversionRate = x.ConversionRate
            });

        var compactData = new
        {
            report = new
            {
                projectId = task.ProjectId,
                periodStart = task.StartDate.ToString("dd.MM.yyyy"),
                periodEnd = task.EndDate.ToString("dd.MM.yyyy")
            },
            summary = new
            {
                totalClicks = statistics.TotalClicks,
                totalSubscriptions = statistics.TotalSubscriptions,
                uniqueUsers = statistics.UniqueUsers,
                conversionRate = statistics.ConversionRate
            },
            dailyStats,
            sourceStats,
            campaignStats
        };

        return $"""
        Проанализируй статистику Telegram-воронки и подготовь содержимое для отчета.

        Данные в JSON:
        {JsonSerializer.Serialize(compactData)}

        Требования:
        1. Не выдумывай цифры.
        2. Не пиши слишком длинно.
        3. KeyFindings: 3-5 пунктов.
        4. Recommendations: 3-5 пунктов.
        5. Risks: 2-4 пункта.
        """;
    }

    private static AiReportContent GetFallbackContent(ProjectStatistics statistics)
    {
        return new AiReportContent
        {
            ExecutiveSummary =
                $"За выбранный период получено {statistics.TotalClicks} кликов, " +
                $"{statistics.TotalSubscriptions} подписок, " +
                $"конверсия составила {statistics.ConversionRate:F2}%.",

            KeyFindings = new List<string>
            {
                "AI-анализ временно недоступен, поэтому показана базовая автоматическая сводка.",
                $"Всего уникальных пользователей: {statistics.UniqueUsers}.",
                $"Всего кликов: {statistics.TotalClicks}.",
                $"Всего подписок: {statistics.TotalSubscriptions}."
            },
        
            Recommendations = new List<string>
            {
                "Проверьте источники трафика с высокой кликабельностью и низкой конверсией.",
                "Сравните эффективность кампаний между собой.",
                "Обратите внимание на дни с резким падением подписок."
            },

            Risks = new List<string>
            {
                "AI-сервис не вернул анализ, отчет сформирован без расширенных выводов.",
                "Для точных рекомендаций нужно проверить динамику по источникам и кампаниям."
            }
        };
    }
}