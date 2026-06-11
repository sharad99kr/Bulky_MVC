using MediatR;

namespace ProjectCore.CQRS.Queries
{
    public record GetConversationQuery(Guid conversationId, string UserId) :
                    IRequest<IEnumerable<ChatTurnDto>>;
    public record ChatTurnDto(string Role, string Content);
}
