namespace ProjectCore.Services.AI
{
    public interface IAzureSearchIndexService
    {
        Task IndexAllProductsAsync(CancellationToken ct=default);
        Task EnsureIndexExistsAsync(CancellationToken ct=default);
    }
}
