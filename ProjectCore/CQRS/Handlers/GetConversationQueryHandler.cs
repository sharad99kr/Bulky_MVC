using Bulky.DataAccess.Repository.IRepository;
using MediatR;
using NuGet.Protocol.Plugins;
using ProjectCore.CQRS.Queries;

namespace ProjectCore.CQRS.Handlers
{
    public class GetConversationQueryHandler : IRequestHandler
                        <GetConversationQuery, 
                         IEnumerable<ChatTurnDto>>
    {
        private readonly IChatMessageRepository _repo;

        public GetConversationQueryHandler(IChatMessageRepository repo) {
            _repo = repo;
        }

        public async Task<IEnumerable<ChatTurnDto>>Handle(
                                GetConversationQuery request,
                                CancellationToken cancellationToken) {

            var turns = await _repo.GetRecentAsync(
                request.conversationId,
                request.UserId,
                count: 50);

            return turns.Select(t => new ChatTurnDto(t.Role, t.Content));
        }
    }
}
