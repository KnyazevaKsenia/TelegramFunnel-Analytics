using Telegram_Analytic.Infrastructure.Interfaces;

namespace Telegram_Analytic.Services;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

public class TgBotPollingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TgBotPollingService> _logger;
    private readonly ITelegramBotClient _bot;

    public TgBotPollingService(IServiceScopeFactory scopeFactory, ILogger<TgBotPollingService> logger, ITelegramBotClient botClient)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _bot = botClient;
    }
    
    public async Task Start()
    {
        await _bot.DeleteWebhook();
        var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
        };
        
        _bot.StartReceiving(async (client, update, token) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var tgBotService = scope.ServiceProvider.GetRequiredService<ITgBotService>();

            await tgBotService.HandleStartCommand(update);

        }, HandleErrorAsync, receiverOptions, cancellationToken: cts.Token);
    }
    
    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Ошибка в Telegram polling");
        return Task.CompletedTask;
    }
}
