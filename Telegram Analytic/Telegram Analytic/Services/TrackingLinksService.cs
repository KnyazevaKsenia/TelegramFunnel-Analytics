using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models;

namespace Telegram_Analytic.Services;

using System;
using System.Text.RegularExpressions;

public class TrackingLinkService : ITrackingLinksService
{
    public TrackingLink CreateTrackingLink(
        string name, 
        string baseUrl, 
        Guid projectId,
        string utmSource,
        string utmCampaign,
        string utmContent)
    {
        // Валидация входных параметров
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название ссылки не может быть пустым", nameof(name));
            
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Базовый URL не может быть пустым", nameof(baseUrl));
            
        if (projectId == Guid.Empty)
            throw new ArgumentException("ID проекта не может быть пустым", nameof(projectId));

        // Генерация уникального идентификатора ссылки
        var urlIdentifier = GenerateUrlIdentifier(name);
        
        // Создание UTM-кампании, если не указана
        utmCampaign ??= GenerateUtmCampaign(name);
        
        // Генерация полного URL с UTM-параметрами
        var generatedUrl = GenerateFullUrl(baseUrl, urlIdentifier, utmSource, utmCampaign, utmContent);

        // Создание объекта TrackingLink
        var trackingLink = new TrackingLink
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            BaseUrl = baseUrl.Trim(),
            UrlIdentifier = urlIdentifier,
            GeneratedUrl = generatedUrl,
            UtmSource = utmSource,
            UtmCampaign = utmCampaign,
            UtmContent = utmContent,
            IsActive = true,
            ClickCount = 0,
            CreatedAt = DateTime.UtcNow,
            ProjectId = projectId
        };

        return trackingLink;
    }

    public string GenerateUrlIdentifier(string name)
    {
        // Транслитерация кириллицы в латиницу
        var transliterated = Transliterate(name);
        
        // Удаление всех недопустимых символов
        var cleaned = Regex.Replace(transliterated, @"[^a-zA-Z0-9\-_]", "-");
        
        // Удаление повторяющихся дефисов
        cleaned = Regex.Replace(cleaned, @"-+", "-");
        
        // Удаление дефисов в начале и конце
        cleaned = cleaned.Trim('-');
        
        // Ограничение длины и добавление случайного суффикса для уникальности
        if (cleaned.Length > 30)
            cleaned = cleaned.Substring(0, 30);
            
        var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        return $"{cleaned}-{randomSuffix}";
    }

    public string GenerateUtmCampaign(string name)
    {
        var cleaned = Regex.Replace(name, @"[^a-zA-Z0-9\-_]", "-");
        cleaned = Regex.Replace(cleaned, @"-+", "-");
        cleaned = cleaned.Trim('-');
        
        return cleaned.Length > 50 ? cleaned.Substring(0, 50) : cleaned;
    }

    public string GenerateFullUrl(string baseUrl, string urlIdentifier, string utmSource, string utmCampaign, string utmContent)
    {
        var uriBuilder = new UriBuilder(baseUrl);
        
        // Добавление параметров к существующему query string
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        query["utm_source"] = utmSource;
        query["utm_medium"] = "click";
        query["utm_campaign"] = utmCampaign;
        
        if (!string.IsNullOrWhiteSpace(utmContent))
            query["utm_content"] = utmContent;
            
        query["tid"] = urlIdentifier; // tracking identifier

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }

    private string Transliterate(string text)
    {
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
}