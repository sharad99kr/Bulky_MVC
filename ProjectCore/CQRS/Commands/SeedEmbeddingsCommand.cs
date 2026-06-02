using MediatR;

namespace ProjectCore.CQRS.Commands
{
    public record SeedEmbeddingsCommand
    (IEnumerable<int>? ProductIds = null): IRequest<int>;
}
