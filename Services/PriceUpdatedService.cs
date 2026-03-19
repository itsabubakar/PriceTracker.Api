using Hangfire;
using Microsoft.EntityFrameworkCore;

public class PriceUpdatedService
{
    private readonly AppDbContext _db;
    private readonly ScraperService _scraper;
    private readonly ILogger<PriceUpdatedService> _logger;
    private readonly IBackgroundJobClient _jobs;

    public PriceUpdatedService(
        AppDbContext db,
        ScraperService scraper,
        ILogger<PriceUpdatedService> logger,
        IBackgroundJobClient jobs)
    {
        _db = db;
        _scraper = scraper;
        _logger = logger;
        _jobs = jobs;
    }

    public async Task UpdateSingleProduct(int productId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return;

        _logger.LogInformation("Updating product {ProductId}", productId);

        var scraped = await _scraper.GetProduct(product.Url, product.Store);
        if (scraped == null) return;

        if (string.IsNullOrWhiteSpace(product.Name))
            product.Name = scraped.Name;

        if (product.Price != scraped.Price)
        {
            product.Price = scraped.Price;
            product.PriceFormatted = scraped.PriceFormatted;

            _db.PriceHistories.Add(new PriceHistory
            {
                ProductId = product.Id,
                Price = scraped.Price ?? 0,
                CreatedAt = DateTime.UtcNow
            });
        }

        product.LastChecked = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Finished updating product {ProductId}", productId);
    }

    public async Task UpdateAllPrices()
    {
        var productIds = await _db.Products
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var id in productIds)
        {
            // Queue each product separately (better scaling)
            _jobs.Enqueue<PriceUpdatedService>(x => x.UpdateSingleProduct(id));
        }
    }
}
