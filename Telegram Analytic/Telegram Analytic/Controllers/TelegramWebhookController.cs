using Microsoft.AspNetCore.Mvc;
using Telegram_Analytic.Infrastructure.Interfaces;
using Telegram_Analytic.Services;
using Telegram.Bot.Types;

namespace Telegram_Analytic.Controllers;
 
[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly ITgBotService _botService;
    private readonly ILogger<TelegramWebhookController> _logger;
    public TelegramWebhookController(ITgBotService botService, ILogger<TelegramWebhookController> logger)
    {
        _botService = botService;
        _logger = logger;
    }
    
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook([FromBody] Update update)
    {
        _logger.LogInformation($"Received update: {update.Id}");
        
        try
        {
            if (update.Message?.Text?.StartsWith("/start") == true)
            {
                await _botService.HandleStartCommand(update);
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook update");
            return StatusCode(500);
        }
    }
}

