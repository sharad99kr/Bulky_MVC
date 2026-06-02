using MediatR;

namespace ProjectCore.CQRS.Commands
{
    public record TriggerInventoryCheckCommand() : IRequest;

}
