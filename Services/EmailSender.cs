using Microsoft.Extensions.Options;
using PriceTracker.Api.Configuration;
using Resend;


public class EmailSender : IEmailSender
{

    private readonly ILogger<EmailSender> _logger;
    private readonly IResend _resend;
    private readonly EmailOptions _emailOptions;

    public EmailSender(
      ILogger<EmailSender> logger,
      IResend resend,
      IOptions<EmailOptions> emailOptions)
    {
        _logger = logger;
        _resend = resend;
        _emailOptions = emailOptions.Value;
    }
    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var message = new EmailMessage
        {
            From = string.IsNullOrWhiteSpace(_emailOptions.FromName)
                ? _emailOptions.FromEmail
                : $"{_emailOptions.FromName} <{_emailOptions.FromEmail}>",
            Subject = subject,
            HtmlBody = body
        };

        message.To.Add(toEmail);

        try
        {
            await _resend.EmailSendAsync(message);
            _logger.LogInformation("Email sent to {ToEmail} with subject {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }

}