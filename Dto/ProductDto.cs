using System.Text.Json.Serialization;

public class ProductDto
{
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    [JsonPropertyName("price_formatted")]
    public required string PriceFormatted { get; set; }
    public required string Url { get; set; }
    public required string OwnerKey { get; set; }
}