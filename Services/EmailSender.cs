public class EmailSender : IEmailSender
{

    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }
    public Task SendAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation(
            "EMAIL -> To: {To}, Subject: {Subject}, Body: {Body}",
            toEmail, subject, body);

        return Task.CompletedTask;
    }

}