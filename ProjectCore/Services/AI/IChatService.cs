namespace ProjectCore.Services.AI
{
    public interface IChatService
    {
        Task<ChatResponse> SendMessageAsync(
            string userMessage,
            Guid? conversationId,
            string userId,
            CancellationToken cancellationToken = default
        );
    }
}

//ChatTurn is kept for internal use only - not sent over wire
public record ChatTurn(
    string Role,
    String Content
);

public record ChatResponse(
    string Message,
    Guid ConversationId,
    bool FromCache,
    bool FallbackUsed,
    int TokensUsed,
    string? WarningMessage = null
);
