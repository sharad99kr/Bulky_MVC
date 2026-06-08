using MediatR;
using ProjectCore.CQRS.Commands;
using ProjectCore.Services.AI;

namespace ProjectCore.CQRS.Handlers
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatResponse>
    {
        private readonly IChatService _chatService;
        public SendMessageCommandHandler(IChatService chatService)
        {
            _chatService = chatService;
        }
        public Task<ChatResponse> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            return _chatService.SendMessageAsync(request.UserMessage, request.History, cancellationToken);
        }
    
    }
}
