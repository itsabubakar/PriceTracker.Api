public class ProductFetchService : IProductFetchService
{
    public Task<Product> FetchProductAsync(string url)
    {
        var store = url.Contains("jumia") ? "Jumia" : "Konga";
        var randomPrice = new Random().Next(1000, 10000);

        var product = new Product
        {
            Url = url,
            Name = "Sample Product",
            Price = randomPrice,
            Store = store
        };

        return Task.FromResult(product);
    }
}