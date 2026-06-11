using MediatR;

namespace ProjectCore.CQRS.Commands
{
    public record SendMessageCommand
    (
        string UserMessage,
        Guid? ConversationId,
        string UserId
    ) : IRequest<ChatResponse>;
}
