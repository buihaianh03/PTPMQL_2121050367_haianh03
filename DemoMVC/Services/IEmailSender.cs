namespace DemoMVC.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string recipient, string subject, string htmlMessage);
}
