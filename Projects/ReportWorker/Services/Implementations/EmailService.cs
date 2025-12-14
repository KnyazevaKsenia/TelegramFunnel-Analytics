using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using CommonRabbitMq;
using Microsoft.Extensions.Options;
using TelegramFunnelAnalytics.ReportWorker.Services.Interfaces;


namespace TelegramFunnelAnalytics.ReportWorker.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        
        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            
            ValidateSettings();
        }
        
        public async Task<bool> SendReportAsync(ReportTask task, ReportResult result, ReportFormat format)
        {
            try
            {
                _logger.LogInformation(" Отправка {Format} отчета {ReportId} на {Email}", 
                    format, task.ReportId, task.Email);
                
                using var smtpClient = CreateSmtpClient();
                using var mailMessage = new MailMessage();
                
                mailMessage.From = new MailAddress(
                    _settings.SenderEmail.Trim(), 
                    _settings.SenderName?.Trim() ?? "Telegram Funnel Analytics");
                
                _logger.LogInformation("Отправитель: {FromAddress}", mailMessage.From.Address);
                
                mailMessage.Subject = GetSubject(task, format);
                mailMessage.Body = GetBody(task, result, format);
                mailMessage.IsBodyHtml = true;
                
                
                try
                {
                    mailMessage.To.Add(new MailAddress(task.Email.Trim()));
                }
                catch (FormatException ex)
                {
                    _logger.LogError(ex, "Неверный формат email: {Email}", task.Email);
                    return false;
                }
                using var memoryStream = new MemoryStream(result.FileBytes);
                var attachment = new Attachment(memoryStream, result.FileName, GetMimeType(format));
                if (attachment.ContentDisposition != null)
                {
                    attachment.ContentDisposition.FileName = result.FileName;
                }
                mailMessage.Attachments.Add(attachment);
                
                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation(" Отчет {ReportId} успешно отправлен", task.ReportId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Ошибка отправки отчета {ReportId}", task.ReportId);
                return false;
            }
        }
        
        public async Task<bool> SendErrorAsync(ReportTask task, string errorMessage)
        {
            try
            {
                _logger.LogWarning("Отправка email об ошибке для отчета {ReportId}", task.ReportId);
                
                if (string.IsNullOrWhiteSpace(task.Email))
                {
                    _logger.LogError("Email получателя пустой для отправки ошибки отчета {ReportId}", task.ReportId);
                    return false;
                }
                
                using var smtpClient = CreateSmtpClient();
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(
                        _settings.SenderEmail.Trim(), 
                        _settings.SenderName?.Trim() ?? "Telegram Funnel Analytics"),
                    Subject = "Ошибка генерации отчета - Telegram Funnel Analytics",
                    Body = GetErrorBody(task, errorMessage),
                    IsBodyHtml = true
                };
                
                mailMessage.To.Add(new MailAddress(task.Email.Trim()));
                
                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation(" Email об ошибке для отчета {ReportId} отправлен", task.ReportId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки email с ошибкой для отчета {ReportId}", 
                    task.ReportId);
                return false;
            }
        }
        
        private SmtpClient CreateSmtpClient()
        {
            var smtpClient = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                UseDefaultCredentials = false, // Важно для Gmail!
                Credentials = new NetworkCredential(
                    _settings.SenderEmail.Trim(), 
                    _settings.SenderPassword),
                Timeout = _settings.Timeout,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            
            
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            return smtpClient;
        }
        
        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            {
                throw new InvalidOperationException("SenderEmail не настроен в appsettings.json");
            }
            
            if (string.IsNullOrWhiteSpace(_settings.SenderPassword))
            {
                throw new InvalidOperationException("SenderPassword не настроен в appsettings.json");
            }
            
            if (!IsValidEmail(_settings.SenderEmail))
            {
                throw new InvalidOperationException($"Неверный формат email: {_settings.SenderEmail}");
            }
            
            _logger.LogInformation("Настройки email проверены: {Email}", _settings.SenderEmail);
        }
        
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        private string GetSubject(ReportTask task, ReportFormat format)
        {
            var formatName = format == ReportFormat.Excel ? "Excel" : "PDF";
            return $"📊 Ваш {formatName} отчет готов - Telegram Funnel Analytics";
        }
        
        private string GetBody(ReportTask task, ReportResult result, ReportFormat format)
        {
            var formatName = format == ReportFormat.Excel ? "Excel" : "PDF";
            var fileSizeKB = result.FileSize > 0 ? (result.FileSize / 1024) : 0;
            
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px;'>
                    <h2 style='color: #2c3e50;'>📊 Ваш {formatName} отчет готов!</h2>
                    
                    <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <p><strong>🔹 Проект:</strong> {task.ProjectId}</p>
                        <p><strong>🔹 Период:</strong> {task.StartDate:dd.MM.yyyy} - {task.EndDate:dd.MM.yyyy}</p>
                        <p><strong>🔹 Формат:</strong> {formatName}</p>
                        <p><strong>🔹 Размер файла:</strong> {fileSizeKB:N0} KB</p>
                        <p><strong>🔹 Дата генерации:</strong> {result.GeneratedAt:dd.MM.yyyy HH:mm}</p>
                    </div>
                    
                    <p>Отчет прикреплен к этому письму в виде вложения.</p>
                    
                    <div style='margin-top: 20px; padding-top: 15px; border-top: 1px solid #dee2e6; color: #6c757d; font-size: 12px;'>
                        <p>Это письмо сгенерировано автоматически. Пожалуйста, не отвечайте на него.</p>
                        <p>Telegram Funnel Analytics © {DateTime.Now.Year}</p>
                    </div>
                </div>
            </body>
            </html>";
        }
        
        private string GetErrorBody(ReportTask task, string errorMessage)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='background-color: #fff3cd; padding: 20px; border-radius: 5px; border: 1px solid #ffeaa7;'>
                    <h2 style='color: #856404;'> Произошла ошибка при генерации отчета</h2>
                    
                    <div style='background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <p><strong>🔹 Проект:</strong> {task.ProjectId}</p>
                        <p><strong>🔹 Период:</strong> {task.StartDate:dd.MM.yyyy} - {task.EndDate:dd.MM.yyyy}</p>
                    </div>
                    
                    <p>Пожалуйста, попробуйте сгенерировать отчет еще раз или обратитесь в поддержку.</p>
                    
                    <div style='margin-top: 20px; padding-top: 15px; border-top: 1px solid #dee2e6; color: #6c757d; font-size: 12px;'>
                        <p>Telegram Funnel Analytics © {DateTime.Now.Year}</p>
                    </div>
                </div>
            </body>
            </html>";
        }
        
        private string GetMimeType(ReportFormat format)
        {
            return format switch
            {
                ReportFormat.Pdf => MediaTypeNames.Application.Pdf,
                ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => MediaTypeNames.Application.Octet
            };
        }
    }
}