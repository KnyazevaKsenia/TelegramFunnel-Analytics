using System.Web;
using Microsoft.EntityFrameworkCore;
using Telegram_Analytic.Infrastructure.Database;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models;
using System.Text.RegularExpressions;

namespace Telegram_Analytic.Services;
public class TrackingLinksService : ITrackingLinksService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TrackingLinksService> _logger;
    private readonly ApplicationDbContext _context;

    public TrackingLinksService(
        IConfiguration configuration, 
        ILogger<TrackingLinksService> logger,
        ApplicationDbContext context)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
    }

    public async Task<TrackingLink> ProcessClickAsync(string identifier)
    {
        try
        {
            _logger.LogInformation("Обработка клика по идентификатору: {Identifier}", identifier);

            var trackingLink = await _context.TrackingLinks
                .Include(tl => tl.Project)
                .FirstOrDefaultAsync(tl => tl.UrlIdentifier == identifier && tl.IsActive);
            
            if (trackingLink == null)
            {
                _logger.LogWarning("Ссылка не найдена или неактивна: {Identifier}", identifier);
                return null;
            }

            trackingLink.ClickCount++;
            trackingLink.LastClickedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Клик обработан. Ссылка: {LinkId}, Кликов: {ClickCount}", 
                trackingLink.Id, trackingLink.ClickCount);

            return trackingLink;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке клика: {Identifier}", identifier);
            throw;
        }
    }

    public TrackingLink CreateTrackingLink(
        string name,
        string baseUrl,  // Чистый URL без параметров
        Guid projectId,
        string utmSource,
        string utmCampaign,
        string utmContent)
    {
        ValidateInputParameters(name, baseUrl, projectId);

        // Генерируем идентификатор для трекинговой ссылки
        var urlIdentifier = GenerateUrlIdentifier(name);
        
        // Генерируем UTM кампанию если не указана
        utmCampaign ??= GenerateUtmCampaign(name);
        
        // Создаем конечный URL с UTM-параметрами
        var finalUrl = BuildFinalUrlWithUtm(baseUrl, utmSource, utmCampaign, utmContent);
        
        // Создаем трекинговый URL (простой, без UTM)
        var trackingUrl = GenerateTrackingUrl(urlIdentifier);

        var trackingLink = new TrackingLink
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            BaseUrl = finalUrl,  // Сохраняем URL с UTM-параметрами
            UrlIdentifier = urlIdentifier,
            GeneratedUrl = trackingUrl,  // Трекинговый URL (простой)
            UtmSource = utmSource?.Trim(),
            UtmCampaign = utmCampaign?.Trim(),
            UtmContent = utmContent?.Trim(),
            IsActive = true,
            ClickCount = 0,
            CreatedAt = DateTime.UtcNow,
            ProjectId = projectId
        };

        _logger.LogInformation("Создана трекинг ссылка: {LinkId} для проекта {ProjectId}", 
            trackingLink.Id, projectId);

        return trackingLink;
    }
    
    public string GenerateUrlIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название не может быть пустым");

        var transliterated = Transliterate(name);
        
        var cleaned = Regex.Replace(transliterated, @"[^a-zA-Z0-9\-_]", "-");
        cleaned = Regex.Replace(cleaned, @"-+", "-");
        cleaned = cleaned.Trim('-');
        
        if (cleaned.Length > 30)
            cleaned = cleaned.Substring(0, 30);
            
        var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        return $"{cleaned}-{randomSuffix}".ToLower();
    }
    
    public string GenerateUtmCampaign(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "default-campaign";

        var cleaned = Regex.Replace(name, @"[^a-zA-Z0-9\-_]", "-");
        cleaned = Regex.Replace(cleaned, @"-+", "-");
        cleaned = cleaned.Trim('-');
        
        return cleaned.Length > 50 ? cleaned.Substring(0, 50).ToLower() : cleaned.ToLower();
    }

    public string GenerateFullUrl(string baseUrl, string urlIdentifier, string utmSource, string utmCampaign, string utmContent)
    {
        // Этот метод теперь создает ПРОСТОЙ трекинговый URL без UTM
        var trackingDomain = _configuration["Tracking:Domain"] ?? "https://track.yourdomain.com";
        return $"{trackingDomain}/click/{urlIdentifier}";
    }

    #region Private Methods

    private void ValidateInputParameters(string name, string baseUrl, Guid projectId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название ссылки не может быть пустым", nameof(name));
            
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Базовый URL не может быть пустым", nameof(baseUrl));
            
        if (projectId == Guid.Empty)
            throw new ArgumentException("ID проекта не может быть пустым", nameof(projectId));

        // Проверяем базовый URL без параметров
        var cleanBaseUrl = baseUrl.Split('?')[0];
        if (!Uri.IsWellFormedUriString(cleanBaseUrl, UriKind.Absolute))
            throw new ArgumentException("Некорректный формат базового URL", nameof(baseUrl));
    }

    private string GenerateTrackingUrl(string urlIdentifier)
    {
        // Простой URL трекера без параметров
        var trackingDomain = _configuration["Tracking:Domain"] ?? "https://track.yourdomain.com";
        return $"{trackingDomain}/click/{urlIdentifier}";
    }

    private string BuildFinalUrlWithUtm(string baseUrl, string utmSource, string utmCampaign, string utmContent)
    {
        // Создаем конечный URL с UTM-параметрами
        var uriBuilder = new UriBuilder(baseUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        
        // Добавляем UTM-параметры к конечному URL
        if (!string.IsNullOrEmpty(utmSource))
            query["utm_source"] = utmSource;
        if (!string.IsNullOrEmpty(utmCampaign))
            query["utm_campaign"] = utmCampaign;
        if (!string.IsNullOrEmpty(utmContent))
            query["utm_content"] = utmContent;
            
        query["utm_medium"] = "click";

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }

    private string Transliterate(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var translitMap = new Dictionary<string, string>
        {
            {"а", "a"}, {"б", "b"}, {"в", "v"}, {"г", "g"}, {"д", "d"},
            {"е", "e"}, {"ё", "yo"}, {"ж", "zh"}, {"з", "z"}, {"и", "i"},
            {"й", "y"}, {"к", "k"}, {"л", "l"}, {"м", "m"}, {"н", "n"},
            {"о", "o"}, {"п", "p"}, {"р", "r"}, {"с", "s"}, {"т", "t"},
            {"у", "u"}, {"ф", "f"}, {"х", "h"}, {"ц", "ts"}, {"ч", "ch"},
            {"ш", "sh"}, {"щ", "sch"}, {"ъ", ""}, {"ы", "y"}, {"ь", ""},
            {"э", "e"}, {"ю", "yu"}, {"я", "ya"},
            {"А", "A"}, {"Б", "B"}, {"В", "V"}, {"Г", "G"}, {"Д", "D"},
            {"Е", "E"}, {"Ё", "Yo"}, {"Ж", "Zh"}, {"З", "Z"}, {"И", "I"},
            {"Й", "Y"}, {"К", "K"}, {"Л", "L"}, {"М", "M"}, {"Н", "N"},
            {"О", "O"}, {"П", "P"}, {"Р", "R"}, {"С", "S"}, {"Т", "T"},
            {"У", "U"}, {"Ф", "F"}, {"Х", "H"}, {"Ц", "Ts"}, {"Ч", "Ch"},
            {"Ш", "Sh"}, {"Щ", "Sch"}, {"Ъ", ""}, {"Ы", "Y"}, {"Ь", ""},
            {"Э", "E"}, {"Ю", "Yu"}, {"Я", "Ya"}
        };

        return translitMap.Aggregate(text, (current, pair) => 
            current.Replace(pair.Key, pair.Value));
    }

    #endregion
}