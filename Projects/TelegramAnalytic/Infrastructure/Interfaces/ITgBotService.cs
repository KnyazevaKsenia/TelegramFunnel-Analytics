using Telegram.Bot.Types;

namespace Telegram_Analytic.Infrastructure.Interfaces;

public interface ITgBotService
{
    Task SetTelegramWebhook();
    Task HandleStartCommand(Update update);
    Task<bool> IsBotAdmin(string url);
}

