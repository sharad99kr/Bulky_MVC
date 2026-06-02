
using MediatR;
using ProjectCore.Models.AI;

namespace ProjectCore.CQRS.Commands
{
    public record GenerateDescriptionCommand(
        AIProductDescriptionRequest Request
    ) : IRequest<AIResponse<AIProductDescriptionResult>>;

}

