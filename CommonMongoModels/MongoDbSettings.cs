namespace CommonMongoModels;

public class MongoDbSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string DatabaseName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    
    public string ConnectionString 
    {
        get
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                return $"mongodb://{Host}:{Port}";
            
            return $"mongodb://{Username}:{Password}@{Host}:{Port}/{DatabaseName}?authSource=admin";
        }
    }
}