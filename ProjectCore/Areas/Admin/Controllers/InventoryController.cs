using Bulky.DataAccess.AI.CQRS.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using ProjectCore.CQRS.Commands;

namespace ProjectCore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles ="Admin")]
    public class InventoryController : Controller
    {
        private readonly IMediator _mediator;

        public InventoryController(IMediator mediator) {
            _mediator = mediator;
        }
        public IActionResult Alerts() {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunCheck(CancellationToken ct) {
            var result = await _mediator.Send(new TriggerInventoryCheckCommand(), ct);
            return Ok(result); // { lowStockCount, alertsPublished, briefing }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedStock(CancellationToken ct) {

            var updated = await _mediator.Send(new SeedStockCommand(), ct);
            return Ok(new {updated});

        }
    }
}
