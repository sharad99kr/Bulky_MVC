namespace ProjectCore.Services.AI
{
    public interface IChatService
    {
        Task<ChatResponse> SendMessageAsync(
            string userMessage,
            IEnumerable<ChatTurn> history,
            CancellationToken cancellationToken = default
        );
    }
}

public record ChatTurn(
    string Role,
    String Content
);

public record ChatResponse(
    string Message,
    bool FromCache,
    bool FallbackUsed,
    int TokensUsed,
    string? WarningMessage = null
);
