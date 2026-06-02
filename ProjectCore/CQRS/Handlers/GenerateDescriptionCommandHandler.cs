using MediatR;
using NuGet.Protocol.Plugins;
using ProjectCore.CQRS.Commands;
using ProjectCore.Models.AI;
using ProjectCore.Services.AI;

namespace ProjectCore.CQRS.Handlers
{
    public class GenerateDescriptionCommandHandler : IRequestHandler<GenerateDescriptionCommand, AIResponse<AIProductDescriptionResult>>
    {
        private readonly IProductAIService _productAIService;

        public GenerateDescriptionCommandHandler(IProductAIService productAIService)
        {
            _productAIService = productAIService;
        }

        public Task<AIResponse<AIProductDescriptionResult>> Handle(GenerateDescriptionCommand request, CancellationToken cancellationToken)
        {
            // Call the AI service to generate a product description
            var description = _productAIService.GenerateDescriptionAsync(request.Request, cancellationToken);
            return description;
        }
    }
}
