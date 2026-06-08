using MediatR;

namespace ProjectCore.CQRS.Commands
{
    public record SendMessageCommand
    (
        string UserMessage,
        IEnumerable<ChatTurn> History
    ) : IRequest<ChatResponse>;
}
