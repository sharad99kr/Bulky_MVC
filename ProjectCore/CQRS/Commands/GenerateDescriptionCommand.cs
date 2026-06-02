
using MediatR;

namespace ProjectCore.CQRS.Commands
{
    public record GenerateDescriptionCommand(
        int ProductId,
        string Tone
    ) : IRequest<string>;

}

