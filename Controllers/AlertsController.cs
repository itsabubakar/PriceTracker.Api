namespace PriceTracker.Api.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceTracker.Api.Infrastructure;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertsController : Controller
{

    private static readonly HashSet<int> AllowedFrequencies = [1, 2, 3, 5, 7];
    private readonly AppDbContext _db;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(AppDbContext db, ILogger<AlertsController> logger)
    {
        _db = db;
        _logger = logger;

    }

    [HttpGet("alert")]
    public async Task<IActionResult> GetAlerts()
    {
        if (!GetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user identity" });

        var alerts = await _db.ProductAlerts.Where(x => x.UserId == userId).Select(x => new
        {
            x.Id,
            x.ProductId,
            x.FrequencyDays,
            x.IsEnabled,
            x.LastSentAt,
            x.NextSendAt
        }).ToListAsync();
        return Ok(alerts);
    }
    [HttpPatch("alert")]

    public async Task<IActionResult> CreateOrUpdateAlert(int productId, [FromBody] CreateAlertRequest request)
    {
        if (!GetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user identity" });

        if (!AllowedFrequencies.Contains(request.FrequencyDays))
            return BadRequest(new { message = "Frequency must be one of: 1,2,3,5,7" });

        var productExists = await _db.Products.AnyAsync(p => p.Id == productId && p.OwnerKey == $"user:{userId}");
        if (!productExists)
            return NotFound(new { message = "Product not found for this user" });

        var alert = await _db.ProductAlerts.FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId);

        if (alert is null)
        {
            alert = new ProductAlert
            {
                UserId = userId,
                ProductId = productId,
                FrequencyDays = request.FrequencyDays,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                NextSendAt = CalculateNextSendAt(request.FrequencyDays)
            };

            _db.ProductAlerts.Add(alert);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Alert updated", alertId = alert.Id });


        }
        alert.FrequencyDays = request.FrequencyDays;
        alert.IsEnabled = request.IsEnabled;
        alert.NextSendAt = CalculateNextSendAt(request.FrequencyDays);

        await _db.SaveChangesAsync();
        return Ok(new { message = "Alert updated", alertId = alert.Id });

    }



    [HttpDelete("alert/{alertId:int}")]
    public async Task<IActionResult> DeleteAlert(int alertId)
    {
        if (!GetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user identity" });
        var alert = await _db.ProductAlerts.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == alertId);
        if (alert is null) return NotFound(new { message = "No alert found" });
        _logger.LogInformation("Alert={alert}", alert);
        _db.ProductAlerts.Remove(alert);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private bool GetUserId(out int userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out userId);
    }

    private static DateTime CalculateNextSendAt(int frequencyDays)
        => DateTime.UtcNow.AddDays(frequencyDays);

    public sealed class CreateAlertRequest
    {
        public int FrequencyDays { get; set; }
        public bool IsEnabled { get; set; }
    }

    public sealed class ToggleAlertRequest
    {
        public bool IsEnabled { get; set; }
    }

}