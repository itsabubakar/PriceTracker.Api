public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public decimal? Price { get; set; }

    public string? PriceFormatted { get; set; }

    public string Url { get; set; } = "";

    public string Store { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastChecked { get; set; }
}