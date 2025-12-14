using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using StatisticLibrary.Models.StatisticModels;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;

namespace TelegramFunnelAnalytics.ReportWorker.Services.Implementations;

public class ChartGenerator : IChartGenerator
{
    private const int Width = 1000;
    private const int Height = 450;

    private readonly SKColor BackgroundColor = SKColors.White;
    private readonly SKColor AxisColor = SKColors.Gray;
    private readonly SKColor LineColor = SKColors.DeepSkyBlue;
    private readonly SKColor BarColor = SKColors.SteelBlue;
    private readonly SKColor TextColor = SKColors.Black;

    private readonly SKPaint axisPaint;
    private readonly SKPaint textPaint;
    private readonly SKPaint linePaint;
    private readonly SKPaint barPaint;

    public ChartGenerator()
    {
        axisPaint = new SKPaint { Color = AxisColor, StrokeWidth = 2, IsAntialias = true };
        textPaint = new SKPaint { Color = TextColor, TextSize = 16, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") };
        linePaint = new SKPaint { Color = LineColor, StrokeWidth = 3, IsAntialias = true, Style = SKPaintStyle.Stroke };
        barPaint = new SKPaint { Color = BarColor, IsAntialias = true, Style = SKPaintStyle.Fill };
    }
    
    
    public byte[] GenerateDailyChart(ProjectStatistics stats)
    {
        if (!stats.DailyStats.Any()) return CreateEmptyChart("Нет данных");

        var ordered = stats.DailyStats.OrderBy(x => x.Date).ToList();
        var points = ordered.Select((s, i) => new SKPoint(i, s.Clicks)).ToList();
        var labels = ordered.Select(x => x.Date.ToString("dd.MM")).ToList();

        return DrawLineChart("Активность по дням", points, labels);
    }

    public byte[] GenerateSourcesChart(ProjectStatistics stats)
        => DrawBarChart("Источники трафика", stats.SourceStats.Select(s => s.Source), stats.SourceStats.Select(s => s.Clicks));

    public byte[] GenerateCampaignsChart(ProjectStatistics stats)
        => DrawBarChart("Кампании", stats.CampaignStats.Select(s => s.Campaign), stats.CampaignStats.Select(s => s.Clicks));

    public byte[] GenerateLocationsChart(ProjectStatistics stats)
        => DrawBarChart("География", stats.LocationStats.Select(s => $"{s.Country}-{s.City}"), stats.LocationStats.Select(s => s.Clicks));

    public byte[] GenerateDevicesChart(ProjectStatistics stats)
        => DrawBarChart("Устройства", stats.DeviceStats.Select(s => $"{s.DeviceType} ({s.Browser})"), stats.DeviceStats.Select(s => s.Clicks));

    public byte[] GenerateContentChart(ProjectStatistics stats)
        => DrawBarChart("Контент", stats.ContentStats.Select(s => s.Content), stats.ContentStats.Select(s => s.Clicks));


    private byte[] DrawLineChart(string title, List<SKPoint> rawPoints, List<string> labels)
    {
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(BackgroundColor);

        var chartArea = new SKRect(80, 60, Width - 40, Height - 60);

        DrawAxes(canvas, chartArea);

        float maxY = Math.Max(1, rawPoints.Max(p => p.Y));
        float scaleX = chartArea.Width / Math.Max(1, rawPoints.Count - 1);
        float scaleY = chartArea.Height / maxY;

        var scaledPoints = rawPoints.Select((p, i) =>
            new SKPoint(chartArea.Left + i * scaleX, chartArea.Bottom - p.Y * scaleY)
        ).ToArray();
        
        canvas.DrawText(title, chartArea.Left, 30, textPaint);
        if (scaledPoints.Length > 1)
        {
            var path = new SKPath();
            path.MoveTo(scaledPoints[0]);
            for (int i = 1; i < scaledPoints.Length; i++)
                path.LineTo(scaledPoints[i]);
            canvas.DrawPath(path, linePaint);
        }
        
        for (int i = 0; i < labels.Count; i++)
        {
            float x = chartArea.Left + i * scaleX;
            canvas.Save();
            canvas.Translate(x, chartArea.Bottom + 25);
            string label = labels[i].Length > 8 ? labels[i].Substring(0, 8) + "…" : labels[i];
            canvas.DrawText(label, 0, 0, textPaint);
            canvas.Restore();
        }

        return Encode(bitmap);
    }

    private byte[] DrawBarChart(string title, IEnumerable<string> labelsEnum, IEnumerable<int> valuesEnum)
    {
        var labels = labelsEnum.ToList();
        var values = valuesEnum.ToList();

        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(BackgroundColor);

        var chartArea = new SKRect(80, 60, Width - 40, Height - 60);

        DrawAxes(canvas, chartArea);

        canvas.DrawText(title, chartArea.Left, 30, textPaint);

        if (values.Count == 0) return Encode(bitmap);

        int maxVal = Math.Max(1, values.Max());
        float barWidth = chartArea.Width / Math.Max(1, values.Count);

        for (int i = 0; i < values.Count; i++)
        {
            float height = (values[i] / (float)maxVal) * chartArea.Height;
            float x = chartArea.Left + i * barWidth + 5;
            float y = chartArea.Bottom - height;

            canvas.DrawRect(new SKRect(x, y, x + barWidth - 10, chartArea.Bottom), barPaint);

            string label = labels[i].Length > 12 ? labels[i].Substring(0, 12) + "…" : labels[i];
            
            canvas.Save();
            canvas.Translate(x + (barWidth - 10) / 2, chartArea.Bottom + 20);
            canvas.DrawText(label, -label.Length * 4, 0, textPaint);
            canvas.Restore();
        }

        return Encode(bitmap);
    }

    private void DrawAxes(SKCanvas canvas, SKRect rect)
    {
        canvas.DrawLine(rect.Left, rect.Bottom, rect.Right, rect.Bottom, axisPaint);
        canvas.DrawLine(rect.Left, rect.Top, rect.Left, rect.Bottom, axisPaint);
    }
    
    private byte[] CreateEmptyChart(string msg)
    {
        using var bmp = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        canvas.DrawText(msg, 40, 40, textPaint);
        return Encode(bmp);
    }
    
    private byte[] Encode(SKBitmap bmp)
    {
        using var image = SKImage.FromBitmap(bmp);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
