using Microsoft.EntityFrameworkCore;

public class AlertService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AlertService> _logger;

    public AlertService(AppDbContext db, IEmailSender emailSender, ILogger<AlertService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;

    }

    public async Task ProcessAlert(int alertId)
    {
        var alert = await _db.ProductAlerts.FirstOrDefaultAsync(a => a.Id == alertId);
        if (alert == null || !alert.IsEnabled) return;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == alert.UserId);
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == alert.ProductId);

        if (user == null || product == null)
        {
            _logger.LogWarning("Alert {AlertId} skipped: missing user/product", alertId);
            return;
        }

        if (product.Price is null)
        {
            _logger.LogWarning("Alert {AlertId} skipped: price missing", alertId);
            return;
        }

        var subject = $"Price update: {product.Name}";
        var body =
            $"Product: {product.Name}\n" +
            $"Current Price: {product.PriceFormatted ?? product.Price.Value.ToString("0.00")}\n" +
            $"URL: {product.Url}\n" +
            $"Checked: {DateTime.UtcNow:u}";

        await _emailSender.SendAsync(user.Email, subject, body);

        alert.LastSentAt = DateTime.UtcNow;
        alert.NextSendAt = DateTime.UtcNow.AddDays(alert.FrequencyDays);

        await _db.SaveChangesAsync();
    }
}