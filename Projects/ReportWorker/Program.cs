using CommonMongoModels;
using CommonRabbitMq;
using TelegramFunnelAnalytics.ReportWorker;
using StatisticLibrary.Interfaces;
using StatisticLibrary.Services;
using MongoDB.Driver;
using TelegramFunnelAnalytics.ReportWorker.Services.Implementations;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;
using QuestPDF;
using QuestPDF.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

Settings.License = LicenseType.Community;
Settings.EnableDebugging = true;

builder.Services.AddSingleton<MongoDbContext>();

builder.Services.AddSingleton<HttpClient>(sp => new HttpClient());

builder.Services.AddSingleton<IMongoCollection<ClickEvent>>(sp =>
{
    var dbContext = sp.GetRequiredService<MongoDbContext>();
    return dbContext.Clicks; 
});



// 4. Services
builder.Services.AddScoped<IProjectStatisticManager, ProjectStatisticManager>();
builder.Services.AddScoped<IChartGenerator, ChartGenerator>();
builder.Services.AddScoped<IIpLocationService, IpLocationService>();

builder.Services.AddScoped<IPdfGenerator, PdfGenerator>();
builder.Services.AddScoped<IExcelGenerator, ExcelGenerator>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IReportCoordinator, ReportCoordinator>();

// 6. RabbitMQ Service
builder.Services.AddSingleton<RabbitMqService>();

// 7. Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await CheckRabbitMqConnectionAsync(host.Services);

await host.RunAsync();

static async Task CheckRabbitMqConnectionAsync(IServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var rabbitMqService = scope.ServiceProvider.GetRequiredService<RabbitMqService>();
        await rabbitMqService.StartAsync();
        Console.WriteLine("RabbitMQ подключен");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка RabbitMQ: {ex.Message}");
        throw;
    }
}

