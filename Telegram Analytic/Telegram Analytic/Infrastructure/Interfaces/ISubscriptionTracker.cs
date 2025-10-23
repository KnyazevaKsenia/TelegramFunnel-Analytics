using Telegram.Bot.Types;

namespace Telegram_Analytic.Infrastructure.Interfaces;

public interface ISubscriptionTracker
{
    Task StartTrackingAsync(Chat chat, long userId);
}