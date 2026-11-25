using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace DemoMVC.Services;

public sealed class EmailService : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string recipient, string subject, string htmlMessage)
    {
        var missingSettings = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.Host)) missingSettings.Add("Smtp:Host");
        if (_options.Port is < 1 or > 65535) missingSettings.Add("Smtp:Port");
        if (string.IsNullOrWhiteSpace(_options.UserName)) missingSettings.Add("Smtp:UserName");
        if (string.IsNullOrWhiteSpace(_options.Password)) missingSettings.Add("Smtp:Password");
        if (string.IsNullOrWhiteSpace(_options.SenderEmail)) missingSettings.Add("Smtp:SenderEmail");
        if (missingSettings.Count > 0)
        {
            _logger.LogError(
                "Email was not sent because required SMTP settings are missing: {MissingSettings}.",
                string.Join(", ", missingSettings));
            throw new EmailDeliveryException("Cấu hình gửi email chưa đầy đủ.");
        }

        if (string.Equals(_options.Host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
            && (!MailAddress.TryCreate(_options.UserName, out _)
                || !string.Equals(_options.UserName, _options.SenderEmail, StringComparison.OrdinalIgnoreCase)
                || _options.Port != 587
                || !_options.EnableSsl))
        {
            _logger.LogError(
                "Email was not sent because Gmail SMTP requires an email username, matching sender address, port 587, and STARTTLS.");
            throw new EmailDeliveryException("Cấu hình Gmail SMTP chưa hợp lệ.");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.SenderEmail, _options.SenderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(recipient));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 15000
            };
            client.Credentials = new System.Net.NetworkCredential(_options.UserName, _options.Password);

            await client.SendMailAsync(message);
        }
        catch (SmtpException exception)
        {
            _logger.LogError(
                "SMTP delivery failed with status {SmtpStatusCode} (HRESULT {HResult}).",
                exception.StatusCode,
                exception.HResult);
            throw new EmailDeliveryException("Máy chủ email không chấp nhận gửi thư.");
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(
                "SMTP delivery failed due to an invalid client operation (HRESULT {HResult}).",
                exception.HResult);
            throw new EmailDeliveryException("Không thể khởi tạo kết nối gửi email.");
        }
        catch (FormatException exception)
        {
            _logger.LogError(
                "SMTP delivery failed due to an invalid email address (HRESULT {HResult}).",
                exception.HResult);
            throw new EmailDeliveryException("Địa chỉ email gửi hoặc nhận không hợp lệ.");
        }
    }
}
