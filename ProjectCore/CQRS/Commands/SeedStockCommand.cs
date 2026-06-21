using MediatR;

namespace ProjectCore.CQRS.Commands
{
    public record SeedStockCommand() : IRequest<int>;
   
}
