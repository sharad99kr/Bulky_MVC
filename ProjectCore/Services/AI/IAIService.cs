using ProjectCore.Models.AI;

namespace ProjectCore.Services.AI
{
    // Base contract for all AI operations.
    // Any concrete AI provider (OpenAI, Azure, local model) must implement this.
    public interface IAIService
    {

        // Generate text from a prompt with a system instruction
        Task<AIResponse<string>> GenerateTextAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken ct=default);

        // Check if the AI service is reachable (used for health checks)
        Task<bool> IsAvailableAsync(CancellationToken ct=default);
    }
}
