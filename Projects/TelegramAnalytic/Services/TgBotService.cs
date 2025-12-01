using CommonMongoModels;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Telegram_Analytic.Infrastructure.Database;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;

namespace Telegram_Analytic.Services;

public class TgBotService : ITgBotService
{
    private readonly ApplicationDbContext _context;
    private readonly MongoDbContext _mongoContext;
    private readonly ITelegramBotClient _tgClient;
    private readonly IConfiguration _config;
    private readonly ILogger<TgBotService> _logger;
    private readonly ISubscriptionTracker _subscriptionTracker;
    
    public TgBotService(
        ApplicationDbContext context,
        MongoDbContext mongoContext,
        IConfiguration config,
        ILogger<TgBotService> logger,
        ITelegramBotClient tgClient, 
        ISubscriptionTracker subscriptionTracker)
    {
        _context = context;
        _mongoContext = mongoContext;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;
        _tgClient = tgClient; 
        _subscriptionTracker = subscriptionTracker;
    }
    
    public async Task SetTelegramWebhook()
    {
        var domain = _config["Tracking:Domain"];
        
        if (string.IsNullOrEmpty(domain))
        {
            _logger.LogWarning("Tracking:Domain not configured, webhook not set");
            return;
        }
        
        var webhookUrl = $"{domain}/api/telegram/webhook";
        
        try
        {
            await _tgClient.SetWebhook(
                url: webhookUrl,
                allowedUpdates: new[] { 
                    UpdateType.Message, 
                    UpdateType.CallbackQuery 
                });
            
            _logger.LogInformation($"✅ Webhook установлен: {webhookUrl}");
            
            var me = await _tgClient.GetMe();
            _logger.LogInformation($"🤖 Бот @{me.Username} запущен и готов к работе");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка настройки webhook");
        }
    }
    
    public async Task HandleStartCommand(Update update)
    {
        if (update.Message == null) return;
        
        var chatId = update.Message.Chat.Id;
        var username = update.Message.Chat.Username;
        var firstName = update.Message.Chat.FirstName;
        
        var startParams = update.Message.Text.Split(' ');
        var sessionToken = startParams.Length > 1 ? startParams[1] : null;
        var user_id = update.Message.From.Id;
        
        await NextAfterStart(chatId, username, firstName, sessionToken, user_id);
    }
    
    public async Task<bool> IsBotAdmin(string channelUrl)
    {
        try
        {
            var normalizedUrl = NormalizeChannelUrl(channelUrl);
            var chat = await GetChanelFromUrl(normalizedUrl);
            
            if (chat == null)
                return false;
            
            var chatMember = await _tgClient.GetChatMember(chat.Id, _tgClient.BotId);
            
            return chatMember.Status is ChatMemberStatus.Administrator or ChatMemberStatus.Creator;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке прав бота в канале {ChannelUrl}", channelUrl);
            return false;
        }
    }
    
    public async Task NextAfterStart(long chatId, string username, string firstName, string sessionToken, long user_id)
    {
        var clickEvent = await _mongoContext.Clicks
            .Find(x => x.SessionToken == sessionToken)
            .FirstOrDefaultAsync();
        
        if (clickEvent == null)
        {
            await SendNullMessage(chatId);
            return;
        }
        
        var update = Builders<ClickEvent>.Update
            .Set(x => x.IsLinkedWithTelegram, true)
            .Set(x => x.TelegramUserName, username)
            .Set(x=> x.User_Id, user_id);
        
        await _mongoContext.Clicks.UpdateOneAsync(
            x => x.SessionToken == sessionToken, 
            update);
        
        var link = await _context.TrackingLinks
            .FirstOrDefaultAsync(x => x.Id == clickEvent.LinkId);
        
        var project = await _context.Projects.FindAsync(link.ProjectId);
        var goalUrl = project.TelegramChanelUrl;
        
        var normalizedUrl = NormalizeChannelUrl(goalUrl);
        var chanel = await GetChanelFromUrl(normalizedUrl);
        
        if (link != null & project != null)
        {
            await SendDefaultWelcomeMessage(chatId, goalUrl);
            await _subscriptionTracker.StartTrackingAsync(chanel, user_id);
        }
        else
        {
            await SendNullMessage(chatId);
        }
    }
    
    private async Task<Chat> GetChanelFromUrl(string channelUrl)
    {
        try
        {
            return await _tgClient.GetChat($"@{channelUrl}");
        }
        catch
        {
            try
            {
                return await _tgClient.GetChat(channelUrl);
            }
            catch
            {
                if (long.TryParse(channelUrl, out var chatId))
                {
                    return await _tgClient.GetChat(chatId);
                }
            
                return null;
            }
        }
    }
    
    private string NormalizeChannelUrl(string channelUrl)
    {
        if (string.IsNullOrEmpty(channelUrl))
            return channelUrl;
        
        channelUrl = channelUrl.Replace("https://", "").Replace("http://", "").Replace("www.", "");
    
        if (channelUrl.StartsWith("t.me/"))
            channelUrl = channelUrl.Substring(5);
        
        if (channelUrl.StartsWith("@"))
            channelUrl = channelUrl.Substring(1);
        
        var parts = channelUrl.Split('/');
        return parts[0];
    }
    
    public async Task SendDefaultWelcomeMessage(long chatId, string goalUrl)
    {
        try
        {
            var inlineKeyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl("Перейти в канал 📢", goalUrl)
            );
            
            var welcomeText = "👋 Добро пожаловать!\n\n" +
                              "Нажмите кнопку ниже, чтобы перейти в канал и получить доступ к эксклюзивному контенту.";
            
            await _tgClient.SendMessage(
                chatId: chatId,
                text: welcomeText,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html);
            
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке приветственного сообщения: {ex.Message}");
            await SendNullMessage(chatId);
        }
    }
    
    public async Task SendNullMessage(long chatId)
    {
        await _tgClient.SendMessage(
            chatId: chatId,
            text: "Произошла ошибка, пожалуйста, повторите попытку чуть позже");
    }
    
}
