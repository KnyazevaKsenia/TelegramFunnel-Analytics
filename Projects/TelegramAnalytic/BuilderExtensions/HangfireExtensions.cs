using CommonMongoModels;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;

namespace Telegram_Analytic.BuilderExtensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddProjectHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => 
        {
            config.UsePostgreSqlStorage(configuration.GetConnectionString("DefaultConnection"));
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
            
            config.UseFilter(new AutomaticRetryAttribute { Attempts = 3 });
        });
        
        services.AddHangfireServer(serverOptions =>
        {
            serverOptions.Queues = ["subscriptions"];
            serverOptions.WorkerCount = 2;
            serverOptions.SchedulePollingInterval = TimeSpan.FromSeconds(5);
        });
        
        return services;
    }
}