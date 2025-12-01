


namespace CommonMongoModels;

using MongoDB.Driver;

using Microsoft.Extensions.Options;

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
    public IMongoCollection<ReportStatus> ReportStatuses => _database.GetCollection<ReportStatus>("report_statuses");
    
}
