using Bulky.DataAccess.Repository.IRepository;
using MediatR;
using ProjectCore.CQRS.Commands;
using ProjectCore.Services.AI;

namespace ProjectCore.CQRS.Handlers
{
    public class SeedEmbeddingsCommandHandler : IRequestHandler<SeedEmbeddingsCommand, int>
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IUnitOfWork _unitOfWork;
        public SeedEmbeddingsCommandHandler(IEmbeddingService embeddingService, IUnitOfWork unitOfWork)
        {
            _embeddingService = embeddingService;
            _unitOfWork = unitOfWork;
        }
        public async Task<int> Handle(SeedEmbeddingsCommand request, CancellationToken cancellationToken)
        {
            // Call the embedding service to seed the embeddings
            var ids = request.ProductIds?.ToList() ?? _unitOfWork.Product.GetAll().Select(p=>p.Id).ToList();
            await _embeddingService.GenerateProductEmbeddingsAsync(ids, cancellationToken);
            return ids.Count;
        }
    }
}
