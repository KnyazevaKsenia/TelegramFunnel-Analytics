using CommonMongoModels;
using Hangfire;
using MongoDB.Driver;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram_Analytic.Services;

public class SubscriptionTrackingService : ISubscriptionTracker
{
    private readonly IMongoCollection<ClickEvent> _clickEvents;
    private readonly ITelegramBotClient _botClient;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SubscriptionTrackingService> _logger;
    
    public SubscriptionTrackingService(
        MongoDbContext mongoDatabase,
        ITelegramBotClient botClient,
        IBackgroundJobClient backgroundJobClient,
        ILogger<SubscriptionTrackingService> logger)
    {
        _clickEvents = mongoDatabase.Clicks;
        _botClient = botClient;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }
    
    [Queue("subscriptions")]
    public async Task StartTrackingAsync(Chat chat, long userId)
    {
        try
        {
            var lastClickEvent = await _clickEvents
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();

            if (lastClickEvent == null)
            {
                _logger.LogWarning("No ClickEvent found for user {UserId}", userId);
                return;
            }
    
            _backgroundJobClient.Schedule<SubscriptionTrackingService>(
                x => x.CheckSubscriptionJob(lastClickEvent.Id, userId, chat),
                TimeSpan.FromSeconds(30));
            
            _logger.LogInformation("Started subscription tracking for user {UserId} on channel {Channel}", userId, chat.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start tracking for user {UserId}", userId);
            throw;
        }
    }

    [AutomaticRetry(Attempts = 3)]
    [Queue("subscriptions")]
    public async Task CheckSubscriptionJob(string clickEventId, long userId, Chat chat)
    {
        try
        {
            var clickEvent = await _clickEvents
                .Find(x => x.Id == clickEventId)
                .FirstOrDefaultAsync();

            if (clickEvent == null)
            {
                _logger.LogWarning("ClickEvent not found for ID {ClickEventId}", clickEventId);
                return;
            }

            if (DateTime.UtcNow > clickEvent.Timestamp.AddMinutes(10))
            {
                await HandleTimeout(userId);
                return;
            }

            var isSubscribed = await CheckUserSubscription(userId, chat);

            if (isSubscribed)
            {
                await HandleSuccess(clickEventId, userId, clickEvent.Timestamp);
            }
            else
            {
                _backgroundJobClient.Schedule<SubscriptionTrackingService>(
                    x => x.CheckSubscriptionJob(clickEventId, userId, chat),
                    TimeSpan.FromSeconds(30));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in subscription job for {ClickEventId}", clickEventId);
            throw;
        }
    }
    
    private async Task<bool> CheckUserSubscription(long userId, Chat chat)
    {
        try
        {
            var chatMember = await _botClient.GetChatMember(chat.Id, userId);
            return chatMember.Status is ChatMemberStatus.Member
                or ChatMemberStatus.Administrator
                or ChatMemberStatus.Creator;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check subscription for user {UserId} on channel {Channel}", userId, chat.Username);
            return false;
        }
    }

    private async Task HandleSuccess(string clickEventId, long userId, DateTime redirectTime)
    {
        var timeToSubscribe = DateTime.UtcNow - redirectTime;

        var update = Builders<ClickEvent>.Update
            .Set(x => x.IsSubscribed, true);

        await _clickEvents.UpdateOneAsync(
            x => x.Id == clickEventId,
            update);
        
        _logger.LogInformation("User {UserId} subscribed in {Seconds}s", 
            userId, timeToSubscribe.TotalSeconds);
    }
    
    private async Task HandleTimeout(long userId)
    {
        
        _logger.LogInformation("User {UserId} subscription timeout", userId);
    }
}

