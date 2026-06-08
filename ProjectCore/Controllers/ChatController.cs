using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        public async Task<IActionResult> Send([FromBody] ChatRequest request, CancellationToken cancellationToken) {

            if(string.IsNullOrWhiteSpace(request.Message)
                || request.Message.Length > 500) {
                return BadRequest(new { error = "Message must be between 1 and 500 characters." });
            }
            var response = await _mediator.Send(new CQRS.Commands.SendMessageCommand(request.Message, request.History ?? []),
                                                cancellationToken);
            return Ok(response);
        }
    }

    public record ChatRequest(
        string Message,
        IEnumerable<ChatTurn>? History
    );
}
