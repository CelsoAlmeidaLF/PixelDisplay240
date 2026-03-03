using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Systekna.Kernel.Domain.Interfaces;

namespace Systekna.Kernel.Application.Services;

/// <summary>
/// Mock email service para desenvolvimento - apenas loga os emails
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService>? _logger;

    public MockEmailService(ILogger<MockEmailService>? logger = null)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        if (_logger != null)
        {
            _logger.LogInformation(">>> MOCK EMAIL SENT TO: {To}\nSUBJECT: {Subject}\nBODY: {Body}", to, subject, body);
        }
        else
        {
            Console.WriteLine($">>> MOCK EMAIL SENT TO: {to}\n    SUBJECT: {subject}\n    BODY: {body}");
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Email service real usando SMTP - para producao
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;

    public SmtpEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpSettings = _config.GetSection("Smtp");

        using var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"] ?? "587"))
        {
            Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
            EnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true")
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpSettings["From"] ?? "noreply@systekna.com"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }
}
