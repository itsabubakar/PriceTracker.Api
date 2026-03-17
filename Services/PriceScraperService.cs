using HtmlAgilityPack;
using System.Text.RegularExpressions;

public class ScraperService
{
    private readonly HttpClient _http;

    public ScraperService(HttpClient http)
    {
        _http = http;

        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<Product?> GetProduct(string url, string store)
    {
        var html = await _http.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var name = ExtractName(doc, store);
        var price = ExtractBestPrice(doc, store);

        if (name == null || price == null)
            return null;

        return new Product
        {
            Name = name,
            Price = price.Value,
            Url = url,
            Store = store,
            PriceFormatted = $"₦{price.Value:N0}",
        };
    }

    // -----------------------------
    // NAME EXTRACTION
    // -----------------------------
    private string? ExtractName(HtmlDocument doc, string store)
    {
        HtmlNode? node = null;

        if (store.ToLower() == "jumia")
        {
            node = doc.DocumentNode.SelectSingleNode("//h1");
        }

        if (store.ToLower() == "konga")
        {
            node = doc.DocumentNode
                .SelectSingleNode("//h4[contains(@class,'productDetail_productName')]");
        }

        // Fallbacks (in case structure changes)
        if (node == null)
        {
            node = doc.DocumentNode.SelectSingleNode("//h1");
        }

        if (node == null)
        {
            node = doc.DocumentNode.SelectSingleNode("//h4");
        }

        return node?.InnerText.Trim();
    }

    // -----------------------------
    // PRICE EXTRACTION (SMART)
    // -----------------------------
    private decimal? ExtractBestPrice(HtmlDocument doc, string store)
    {
        var candidates = new List<(decimal price, int score)>();

        // Strategy 1: Known structure (high confidence)
        var structuredNode = GetStructuredPriceNode(doc, store);
        if (structuredNode != null)
        {
            var price = ParsePrice(structuredNode.InnerText);
            if (price != null)
                candidates.Add((price.Value, 100));
        }

        // Strategy 2: Any ₦ text
        var nairaNodes = doc.DocumentNode
            .SelectNodes("//*[contains(text(),'₦') or contains(text(),'8358')]");

        if (nairaNodes != null)
        {
            foreach (var node in nairaNodes)
            {
                var price = ParsePrice(node.InnerText);
                if (price == null) continue;

                int score = 50;

                var className = node.GetAttributeValue("class", "").ToLower();

                // Boost likely real price
                if (className.Contains("price")) score += 30;
                if (className.Contains("current")) score += 20;

                // Penalize old/discount prices
                if (className.Contains("old")) score -= 20;
                if (className.Contains("discount")) score -= 10;

                candidates.Add((price.Value, score));
            }
        }

        if (!candidates.Any())
            return null;

        // Pick highest score, then highest price (avoid picking small fees)
        return candidates
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.price)
            .First()
            .price;
    }

    // -----------------------------
    // STORE-SPECIFIC SELECTORS
    // -----------------------------
    private HtmlNode? GetStructuredPriceNode(HtmlDocument doc, string store)
    {
        if (store.ToLower() == "jumia")
        {
            return doc.DocumentNode
                .SelectSingleNode("//span[contains(@class,'-fs24')]");
        }

        if (store.ToLower() == "konga")
        {
            return doc.DocumentNode
                .SelectSingleNode("//div[contains(@class,'priceBox_priceBoxPrice')]/div");
        }

        return null;
    }

    // -----------------------------
    // CLEAN + PARSE PRICE
    // -----------------------------
    private decimal? ParsePrice(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Extract numbers like ₦120,000
        var match = Regex.Match(text, @"₦?\s?([\d,]+)");

        if (!match.Success)
            return null;

        var clean = match.Groups[1].Value.Replace(",", "");

        if (decimal.TryParse(clean, out var price))
            return price;

        return null;
    }
}