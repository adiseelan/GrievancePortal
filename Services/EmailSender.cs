using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace GrievancePortal.Services;

public interface IApplicationEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>Default sender used when email is disabled. Logs instead of sending.</summary>
public class NoOpEmailSender : IApplicationEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;
    public NoOpEmailSender(ILogger<NoOpEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("[Email disabled] Would send to {To}: {Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}

/// <summary>SMTP sender used only when Email:Enabled = true.</summary>
public class SmtpEmailSender : IApplicationEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
            }

            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            // Never let a mail failure break the request pipeline.
            _logger.LogError(ex, "Failed to send email to {To}", toEmail);
        }
    }
}
