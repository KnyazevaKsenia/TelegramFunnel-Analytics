using Hangfire;
using Hangfire.Mongo;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Newtonsoft.Json;
using Telegram_Analytic.Infrastructure.Database;

using Telegram_Analytic.Models;
using Telegram_Analytic.Services;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();

//POSTGRES
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MONGO DB 
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

//HANGFIRE


builder.Services.AddHangfire(config => 
    config.UseMongoStorage(builder.Configuration["MongoDb:ConnectionString"], "hangfire"));
builder.Services.AddHangfireServer();

// Регистрация вашего сервиса
builder.Services.AddScoped<ISubscriptionTracker, SubscriptionTrackingService>();
builder.Services.AddScoped<SubscriptionTrackingService>();


//SERVICES
builder.Services.AddSingleton<MongoDbContext>(); 
builder.Services.AddScoped<ITrackingLinksService, TrackingLinksService>();
builder.Services.AddScoped<ITgBotService, TgBotService>();
builder.Services.AddSingleton<TgBotPollingService>();

//TELEGRAM
builder.Services.AddSingleton<TelegramBotClient>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var token = config["Telegram:BotToken"];
    if (string.IsNullOrEmpty(token))
        throw new Exception("Telegram Bot Token not configured");
    
    return new TelegramBotClient(token);
});



builder.Services.AddSingleton<ITelegramBotClient>(provider => 
    provider.GetRequiredService<TelegramBotClient>());


//HANGFIRE

builder.Services.AddHangfire((provider, config) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    config.UsePostgreSqlStorage(options => 
        options.UseNpgsqlConnection(connectionString));
});

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"TelegramBot-{Environment.MachineName}";
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "default", "subscriptions" };
});


//IDENTITY
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;

        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// только в продакшене
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
 //   await SetWebhook(app.Services);
//});

//async Task SetWebhook(IServiceProvider services)
//{
    //using var scope = services.CreateScope();
    //var botService = scope.ServiceProvider.GetRequiredService<ITgBotService>();
   // await botService.SetTelegramWebhook();
//}
//app.MapGet("/api/telegram/webhook", () => "Webhook endpoint is ready");


app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapFallbackToController("Index", "Home");

var pollingService = app.Services.GetRequiredService<TgBotPollingService>();
pollingService.Start();


app.Run();

