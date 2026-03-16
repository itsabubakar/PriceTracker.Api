public interface IProductFetchService
{
    Task<Product> FetchProductAsync(string url);
}