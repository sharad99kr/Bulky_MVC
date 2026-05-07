using ProjectCore.Models.AI;

namespace ProjectCore.Services.AI
{
    //Product -specific AI operations, such as generating marketing descriptions, product names, etc.
    //Controllers and application services can depend on this interface for product-related AI features, without needing to know the underlying AI provider or IAIService.
    public interface IProductAIService
    {
        // Generates a marketing description for book/product based on the provided request parameters.
        Task<AIResponse<AIProductDescriptionResult>> GenerateDescriptionAsync(
                AIProductDescriptionRequest request,
                CancellationToken ct = default
            );

        //Returns all tones the generator supports( for the UI dropdown)
        IEnumerable<string> GetAvailableTones();
    }
}
