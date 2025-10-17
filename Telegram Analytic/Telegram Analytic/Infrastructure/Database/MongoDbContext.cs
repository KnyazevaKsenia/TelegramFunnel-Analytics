using Microsoft.Extensions.Options;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Models;
using Telegram_Analytic.Models.ClickAnalyticModels;
using Telegram_Analytic.Models.MongoDb;
using Telegram_Analytic.Services;

namespace Telegram_Analytic.Infrastructure.Database;

using MongoDB.Driver;

using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var mongoSettings = settings.Value;
        var client = new MongoClient(mongoSettings.ConnectionString);
        _database = client.GetDatabase(mongoSettings.DatabaseName);
    }
    
    public IMongoCollection<ClickEvent> Clicks => _database.GetCollection<ClickEvent>("clicks");
    public IMongoCollection<UserSession> UserSessions => _database.GetCollection<UserSession>("user_sessions");
    public IMongoCollection<SubscriptionEvent> Subscriptions => _database.GetCollection<SubscriptionEvent>("subscriptions");
}
