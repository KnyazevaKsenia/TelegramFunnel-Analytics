namespace Telegram_Analytic.Services;

public class MongoDbSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 27017;
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    public string ConnectionString 
    { 
        get
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                return $"mongodb://{Host}:{Port}";
            
            return $"mongodb://{Username}:{Password}@{Host}:{Port}";
        }
    }
}