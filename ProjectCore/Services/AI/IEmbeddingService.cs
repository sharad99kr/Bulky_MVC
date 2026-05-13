namespace ProjectCore.Services.AI
{
    // Services/AI/IEmbeddingService.cs
    public interface IEmbeddingService
    {
        //Convert a text string into a vector of numbers(1536 - dimension float vector) that captures its meaning
        Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct=default);

        //Generate embeddings for a list of products (Title + descriptions + Category) and store them in the database
        Task GenerateProductEmbeddingslAsync(IEnumerable<int> productIds, CancellationToken ct=default);
    }
}
