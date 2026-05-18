using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Stripe.Climate;
using System.Collections.Generic;

namespace ProjectCore.Services.AI
{
    public class AzureSearchIndexService : IAzureSearchIndexService
    {
        private const string IndexName = SD.AzureSearchIndexName;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _searchIndexClient;
        private readonly IEmbeddingService _embedding;
        private readonly ILogger<AzureSearchIndexService> _logger;
        public AzureSearchIndexService(
                                    SearchClient searchClient,
                                    SearchIndexClient searchIndexClient,
                                    IUnitOfWork unitOfWork, 
                                    IEmbeddingService embedding, 
                                    ILogger<AzureSearchIndexService> logger ) {
            
           
            _searchClient = searchClient;
            _searchIndexClient = searchIndexClient;
            _unitOfWork = unitOfWork;
            _embedding = embedding;
            _logger = logger;
        }

        public async Task IndexProductAsync(IEnumerable<int> productIds, CancellationToken ct = default) {
            await EnsureIndexExistsAsync(ct);
            var products = _unitOfWork.Product.GetAll(p => productIds.Contains(p.Id), includeProperties: "Category").ToList();
            await GenerateDocumentAndPush(products, ct);
        }

        public async Task IndexAllProductsAsync(CancellationToken ct = default) {
            
            await EnsureIndexExistsAsync(ct);
            var products = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            await GenerateDocumentAndPush(products, ct);

        }

        private async Task GenerateDocumentAndPush(List<Bulky.Models.Product> products, CancellationToken ct) {
            var documents = new List<SearchDocument>();

            foreach(var product in products) {
                try {

                    var textToIndex = $"{product.Title} by {product.Author}. Category: {product.Category?.Name}. {product.Description}";
                    var embedding = await _embedding.GetEmbeddingAsync(textToIndex, ct);

                    var doc = new SearchDocument {
                        ["id"] = product.Id.ToString(),
                        ["title"] = product.Title,
                        ["description"] = product.Description,
                        ["category"] = product.Category?.Name,
                        ["embedding"] = embedding,
                    };
                    documents.Add(doc);

                } catch(OperationCanceledException) {
                    throw;
                } catch(Exception ex) {
                    _logger.LogWarning(ex,
                     "Failed to generate embedding for product {Id} — skipping.", product.Id);
                }


            }

            if(documents.Count > 0) {
                //Batch upload to Azure Search
                await _searchClient.UploadDocumentsAsync(documents, cancellationToken: ct);
                _logger.LogInformation("Uploaded {Count} products to Azure Search index successfully.", documents.Count);
            } else {
                _logger.LogWarning("No documents to index in Azure AI Search.");

            }
        }

        public async Task EnsureIndexExistsAsync(CancellationToken ct = default) 
        {

            try {
                await _searchIndexClient.GetIndexAsync(IndexName, ct);
                _logger.LogInformation("Azure Search index '{IndexName}' already exists.", IndexName);
                return; // already exists, skip creation
            } catch(RequestFailedException ex) when(ex.Status == 404) {
                _logger.LogInformation("Azure Search index '{IndexName}' not found, creating...", IndexName);
                // fall through to create
            }


            var vectorSearchConfig = new VectorSearch();
            vectorSearchConfig.Profiles.Add(
                new VectorSearchProfile("myHnswProfile", "myHnsw")); 
            vectorSearchConfig.Algorithms.Add(
                new HnswAlgorithmConfiguration("myHnsw"));

            var index = new SearchIndex(IndexName) {
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                    new SearchableField("title") { IsSortable = true, IsFilterable = true },
                    new SearchableField("description"),
                    new SearchField("category", SearchFieldDataType.String) { IsFilterable = true },
                    new VectorSearchField("embedding",  1536, vectorSearchProfileName: "myHnswProfile") 
                },
                VectorSearch = vectorSearchConfig,
            };

            try {
                await _searchIndexClient.CreateOrUpdateIndexAsync(index, cancellationToken: ct);
                _logger.LogInformation("Azure Search index '{IndexName}' created successfully.", IndexName);
            } catch(OperationCanceledException) {
                throw;
            } catch(Exception ex) {
                _logger.LogError(ex, "Failed to create Azure Search index '{IndexName}'", IndexName);
                throw; // rethrow to let caller handle
            }
            
        }
    }
}
