
namespace ProjectCore.Services.AI
{
    public class ChatService : IChatService
    {
        public Task<ChatResponse> SendMessageAsync(string userMessage, IEnumerable<ChatTurn> history, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
    }
}
