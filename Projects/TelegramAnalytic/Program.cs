using CommonMongoModels;
using CommonRabbitMq;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver; 
using StatisticLibrary.Interfaces;
using StatisticLibrary.Services;
using Telegram_Analytic.BuilderExtensions;
using Telegram_Analytic.Infrastructure.AutoMapperProfiles;
using Telegram_Analytic.Infrastructure.Database;
using Telegram_Analytic.Services;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models.AccountModels;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// POSTGRES
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MONGO DB 
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

// Регистрация MongoDB сервисов
var mongoDbSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
builder.Services.AddSingleton<IMongoClient>(serviceProvider => 
    new MongoClient(mongoDbSettings.ConnectionString));

builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDbSettings.DatabaseName);
});

// Регистрация коллекции clicks
builder.Services.AddScoped<IMongoCollection<ClickEvent>>(serviceProvider =>
{
    
    var database = serviceProvider.GetRequiredService<IMongoDatabase>();
    return database.GetCollection<ClickEvent>("clicks"); 
});

builder.Services.AddProjectHangfire(builder.Configuration);

// SERVICES
builder.Services.AddScoped<ISubscriptionTracker, SubscriptionTrackingService>();
builder.Services.AddScoped<SubscriptionTrackingService>();
builder.Services.AddSingleton<MongoDbContext>(); 
builder.Services.AddScoped<ITrackingLinksService, TrackingLinksService>();
builder.Services.AddScoped<ITgBotService, TgBotService>();
builder.Services.AddSingleton<TgBotPollingService>();
builder.Services.AddScoped<IMongoClickService, MongoClickService>();
builder.Services.AddScoped<IProjectMappingService, ProjectMappingService>();
builder.Services.AddHttpClient<IIpLocationService, IpLocationService>();
builder.Services.AddScoped<IProjectStatisticManager, ProjectStatisticManager>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
// AutoMapper
builder.Services.AddAutoMapper(typeof(ProjectProfile).Assembly);

// TELEGRAM
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

//RabbitMq
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));


// IDENTITY
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

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapFallbackToController("Index", "Home");

var pollingService = app.Services.GetRequiredService<TgBotPollingService>();
await pollingService.Start();

using (var scope = app.Services.CreateScope())
{
    var rabbitService = scope.ServiceProvider.GetRequiredService<IRabbitMqService>();
    await rabbitService.StartAsync();
}

app.Run();

