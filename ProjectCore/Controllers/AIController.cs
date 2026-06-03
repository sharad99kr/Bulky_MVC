using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectCore.CQRS.Commands;
using ProjectCore.CQRS.Queries;
using ProjectCore.Models.AI;
using ProjectCore.Services.AI;
using System.Diagnostics;

namespace ProjectCore.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("AI")]
    public class AIController : Controller
    {
        private readonly ILogger<AIController> _logger;
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmbeddingService _embedding;
        private readonly IRagEvaluationService _ragEvaluationService;
        private readonly IAzureSearchIndexService _azureSearchIndexService;
        private readonly IProductAIService _productAIService;
        private readonly ISearchService _searchService;
        public AIController(ILogger<AIController> logger,
                                IMediator mediator,
                                IUnitOfWork unitOfWork,
                                IEmbeddingService embedding,
                                ISearchService searchService,
                                IRagEvaluationService ragEvaluationService,
                                IAzureSearchIndexService azureSearchIndexService,
                                IProductAIService productAIService
                                ) {
            _logger = logger;
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _embedding = embedding;
            _ragEvaluationService = ragEvaluationService;
            _azureSearchIndexService = azureSearchIndexService;
            _productAIService = productAIService;
            _searchService = searchService;
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


            var aiResponse = await _mediator.Send(
                new GenerateDescriptionCommand(request), ct);

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

                var count = await _mediator.Send(new SeedEmbeddingsCommand(), ct);
                if(count == 0)
                    return Ok(new { message = "All products already have embeddings seeded" });
                return Ok(new { message = $"Product embeddings seeded for all {count} successfully" });

            } catch(Exception ex) {

                _logger.LogError(ex, "Error seeding product embeddings");
                return StatusCode(500, new { error = "Failed to seed product embeddings" });

            }
        }

        [HttpPost("SeedAzureSearch")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> SeedAzureSearch(CancellationToken ct = default) {

            try {

                await _azureSearchIndexService.IndexAllProductsAsync(ct);
                return Ok(new { message = "Azure AI Search index seeded successfully!" });

            } catch(Exception ex) {

                _logger.LogError(ex, "Error seeding Azure AI Search index");
                return StatusCode(500, new { error = "Failed to seed Azure AI Search index" });

            }
        }

        // GET /AI/Search?q=cozy+weekend+read — public search endpoint
        [HttpGet("Search")]
        [EnableRateLimiting("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string q, bool expand = false, CancellationToken ct = default) {

            if(string.IsNullOrWhiteSpace(q)) {
                TempData["Error"] = "Search query cannot be empty.";
                return RedirectToAction("Index", "Home");
            }

            if(q.Trim().Length < 3 || q.Trim().Length > 200) {
                TempData["Error"] = "Search query must be between 3 and 200 characters.";
                return RedirectToAction("Index", "Home");
            }

            var searchResult = await _mediator.Send(new SearchProductsQuery(q, TopK: 5, expand), ct);

            //Fire and forget logging of search query and results faithfullness for analytics
            if(searchResult.Items.Count > 0) {
                var context = searchResult.Items.Select(i => $"{i.Title}: {i.Description}").ToList(); //ToList() forces immediate execution before the fire-and-forget call. If result.Items comes from an EF Core query, the DbContext could be disposed by the time the background task tries to enumerate it, causing a runtime exception.
                _ = _ragEvaluationService.ScoreFaithfulnessAsync(q, context, CancellationToken.None);
            }

            ViewBag.LowConfidence = searchResult.LowConfidence;
            ViewBag.TopScore = searchResult.TopScore;
            ViewBag.SearchQuery = q;
            ViewBag.SearchMode = "semantic";
            ViewBag.Expanded = expand;

            return View(searchResult.Items);

        }


        // GET /AI/CompareSearch?q=cozy+mystery
        [HttpGet("CompareSearch")]
        [EnableRateLimiting("CompareSearch")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> CompareSearch(string q, bool expand = false, CancellationToken ct=default) {

            return Ok(new {
                Query = "Currently disabled",

            });
            //var sqlTimer = Stopwatch.StartNew();
            var sqlResults = await _searchService.HybridSearchAsync(q, topK: 5, expand, ct);
            //sqlTimer.Stop();

            //var azureTimer = Stopwatch.StartNew();
            var azureResults = await _searchService.AzureAISearchAsync(q, topK: 5, ct);
            //azureTimer.Stop();
            
            //return Ok(new {
            //    query = q,
            //    sqlResults = new {
            //        items = sqlResults.Items.Select(p=>p.Title),
            //        topScore = sqlResults.TopScore,
            //        timeTaken = sqlTimer.ElapsedMilliseconds,
            //        lowConfidence = sqlResults.LowConfidence
            //    },
            //    azureResults = new {
            //        items = azureResults.Items.Select(p => p.Title),
            //        topScore = azureResults.TopScore,
            //        timeTaken = azureTimer.ElapsedMilliseconds,
            //        lowConfidence = azureResults.LowConfidence
            //    }
            //});
            
        }
    }
}
