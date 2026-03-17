public class PriceUpdatedService
{
    private readonly AppDbContext _db;
    private readonly ScraperService _scraper;

    public PriceUpdatedService(AppDbContext db, ScraperService scraper)
    {
        _db = db;
        _scraper = scraper;
    }

}