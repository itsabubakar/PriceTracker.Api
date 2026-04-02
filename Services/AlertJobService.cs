using Hangfire;
using Microsoft.EntityFrameworkCore;

public class AlertJobService
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<AlertJobService> _logger;

    public AlertJobService(AppDbContext db, IBackgroundJobClient jobs, ILogger<AlertJobService> logger)
    {
        _db = db;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task Run()
    {
        var now = DateTime.UtcNow;
        var dueAlertsId = await _db.ProductAlerts
                        .Where(a => a.IsEnabled && a.NextSendAt <= now)
                        .Select(a => a.Id)
                        .ToListAsync();

        _logger.LogInformation("Alert job found {Count} due alerts", dueAlertsId.Count);

        foreach (var id in dueAlertsId)
        {
            _jobs.Enqueue<AlertService>(x => x.ProcessAlert(id));
        }
    }
}
