using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.Enums;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/trash")]
    public class TrashController : ControllerBase
    {
        private readonly ITrashService _trash;

        public TrashController(ITrashService trash) => _trash = trash;

        // GET /api/trash — entries, notes and owned vaults the caller has soft-deleted.
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var items = await _trash.ListAsync(CurrentUserId, ct);
            return Ok(items);
        }

        [HttpPost("{type}/{id:int}/restore")]
        public async Task<IActionResult> Restore(TrashItemType type, int id, CancellationToken ct)
        {
            var result = await _trash.RestoreAsync(CurrentUserId, type, id, ct);
            return result.Success ? NoContent() : MapError(result.Error);
        }

        [HttpDelete("{type}/{id:int}/purge")]
        public async Task<IActionResult> Purge(TrashItemType type, int id, CancellationToken ct)
        {
            var result = await _trash.PurgeAsync(CurrentUserId, type, id, ct);
            return result.Success ? NoContent() : MapError(result.Error);
        }

        /// <summary>Current user id, taken from the JWT "sub" claim.</summary>
        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub")
                      ?? throw new InvalidOperationException("Missing user id claim."));

        private IActionResult MapError(EntryError error) => error switch
        {
            EntryError.NotFound => NotFound(),
            EntryError.Forbidden => Forbid(),
            _ => Problem()
        };
    }
}
