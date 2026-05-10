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

        public AIController(ILogger<AIController> logger, IProductAIService productAIService) {
            _logger = logger;
            _productAIService = productAIService;
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
    }
}
