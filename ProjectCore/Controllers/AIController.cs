using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectCore.Models.AI;
using ProjectCore.Services.AI;

namespace ProjectCore.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("AI")]
    public class AIController : Controller
    {
        private readonly ILogger<AIController> _logger;
        private readonly IProductAIService _productAIService;
        private readonly ISearchService _searchService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmbeddingService _embedding;

        public AIController(ILogger<AIController> logger, 
                                IProductAIService productAIService, 
                                IUnitOfWork unitOfWork, 
                                IEmbeddingService embedding,
                                ISearchService searchService) {
            
            _logger = logger;
            _productAIService = productAIService;
            _unitOfWork = unitOfWork;
            _embedding = embedding;
            _searchService=searchService;

        }

        //POST : /AI/GenerateDescription
        //This endpoint receives product details and returns an AI-generated description
        [HttpPost("GenerateDescription")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GenerateDescription(
            [FromBody] AIProductDescriptionRequest request,
            CancellationToken ct) {

            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var aiResponse = await _productAIService.GenerateDescriptionAsync(request, ct);
            if(!aiResponse.Success) {
                _logger.LogError(
                    "Description generation failed for {Product}. Error: {Error}",
                    request.ProductName,
                    aiResponse.ErrorMessage);
                return StatusCode(503, new { error = aiResponse.ErrorMessage });
            }

            return Ok(new {
                description = aiResponse.Data!.Description,
                tone = aiResponse.Data.Tone,
                generatedAt = aiResponse.Data.GeneratedAt,
                tokensUsed = aiResponse.TokensUsed,
                fromCache = aiResponse.FromCache
            });

        }

        //GET : /AI/GetTones - populates the tone options in the UI dropdown
        [HttpGet("GetTones")]
        public IActionResult GetTones() {
            var tones = _productAIService.GetAvailableTones();
            return Ok(tones);
        }

        //POST /AI/SeedEmbeddings — admin only, run once
        [HttpPost("SeedEmbeddings")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> SeedEmbeddings(CancellationToken ct) {
            try {

                var productIds = _unitOfWork
                                    .Product
                                    .GetAll()
                                    .Where(p => p.SearchEmbeddingData==null) // Only include un embedded products
                                    .Select(p => p.Id)
                                    .ToList();
                
                if(productIds.Count == 0) {
                    return Ok(new { message = "All products already have embeddings seeded" });
                }

                await _embedding.GenerateProductEmbeddingsAsync(productIds, ct);
                return Ok(new { message = $"Product embeddings seeded for all {productIds.Count}successfully" });

            } catch(Exception ex) {

                _logger.LogError(ex, "Error seeding product embeddings");
                return StatusCode(500, new { error = "Failed to seed product embeddings" });

            }
        }

        // GET /AI/Search?q=cozy+weekend+read — public search endpoint
        [HttpGet("Search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string q, CancellationToken ct) {
            
            if(string.IsNullOrWhiteSpace(q)) {
                //TODO Add a toaster with message "Query cannot be empty";
                return RedirectToAction("Index", "Home");
            }
            
            var searchResult = await _searchService.HybridSearchAsync(q, topK: 5, ct);

            ViewBag.LowConfidence = searchResult.LowConfidence;
            ViewBag.TopScore = searchResult.TopScore;
            ViewBag.SearchQuery = q;

            return View(searchResult.Items);

        }
    }
}
