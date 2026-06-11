using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectCore.CQRS.Queries;
using System.Security.Claims;

namespace ProjectCore.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        public readonly IMediator _mediator;

        public ChatController(IMediator mediator) {
            _mediator = mediator;
        }

        public IActionResult Index() {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("chat")]
        public async Task<IActionResult> Send([FromBody] ChatRequest request,
                                        CancellationToken cancellationToken) {

            if(string.IsNullOrWhiteSpace(request.Message)
                || request.Message.Length > 500) {
                return BadRequest(new { error = "Message must be between 1 and 500 characters." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId == null) {
                return Unauthorized();
            }

            var response = await _mediator.Send(
                new CQRS.Commands.SendMessageCommand(
                    request.Message,
                    request.ConversationId,
                    userId),
                cancellationToken);

            return Ok(response);
        }


        [HttpGet]
        public async Task<IActionResult> History(
            [FromQuery] Guid conversationId,
            CancellationToken ct) {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId == null) {
                return Unauthorized();
            }

            var turns = await _mediator.Send(
                new GetConversationQuery(
                    conversationId, userId),
                ct
                );

            return Ok(turns);
        }

        public record ChatRequest(
            string Message,
            Guid? ConversationId
        );
    }
}
