using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Entries;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class EntriesController : ControllerBase
    {
        private readonly IPasswordEntryService _entries;

        public EntriesController(IPasswordEntryService entries) => _entries = entries;

        // GET /api/entries?vaultId=123&tagId=5 — metadata only, no passwords. tagId filters
        // optionally; favorites sort first. No search param (Sprint 26): Name/Username/Url are
        // ciphertext, so the caller fetches the (tag-filtered) list and searches client-side.
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int vaultId, [FromQuery] int? tagId, CancellationToken ct)
        {
            var result = await _entries.ListAsync(CurrentUserId, vaultId, tagId, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        // GET /api/entries/123 — single entry, password as its ciphertext blob (Sprint 26).
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var result = await _entries.GetAsync(CurrentUserId, id, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EntryCreateRequest request, CancellationToken ct)
        {
            var result = await _entries.CreateAsync(CurrentUserId, request, ct);
            if (!result.Success)
            {
                return MapError(result.Error);
            }
            return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, EntryUpdateRequest request, CancellationToken ct)
        {
            var result = await _entries.UpdateAsync(CurrentUserId, id, request, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _entries.DeleteAsync(CurrentUserId, id, ct);
            return result.Success ? NoContent() : MapError(result.Error);
        }

        // PUT /api/entries/123/tags/5 — assign one of the caller's tags to the entry (idempotent).
        [HttpPut("{id:int}/tags/{tagId:int}")]
        public async Task<IActionResult> AddTag(int id, int tagId, CancellationToken ct)
        {
            var result = await _entries.AddTagAsync(CurrentUserId, id, tagId, ct);
            return result.Success ? NoContent() : MapError(result.Error);
        }

        [HttpDelete("{id:int}/tags/{tagId:int}")]
        public async Task<IActionResult> RemoveTag(int id, int tagId, CancellationToken ct)
        {
            var result = await _entries.RemoveTagAsync(CurrentUserId, id, tagId, ct);
            return result.Success ? NoContent() : MapError(result.Error);
        }

        // PUT /api/entries/123/favorite — mark as favorite (idempotent). DELETE unmarks it.
        [HttpPut("{id:int}/favorite")]
        public async Task<IActionResult> SetFavorite(int id, CancellationToken ct)
        {
            var result = await _entries.SetFavoriteAsync(CurrentUserId, id, true, ct);
            return result.Success ? NoContent() : MapError(result.Error);
        }

        [HttpDelete("{id:int}/favorite")]
        public async Task<IActionResult> UnsetFavorite(int id, CancellationToken ct)
        {
            var result = await _entries.SetFavoriteAsync(CurrentUserId, id, false, ct);
            return result.Success ? NoContent() : MapError(result.Error);
        }

        // GET /api/entries/123/history — past passwords, metadata only (no passwords), newest first.
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetHistory(int id, CancellationToken ct)
        {
            var result = await _entries.GetHistoryAsync(CurrentUserId, id, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        // GET /api/entries/123/history/456 — a single past password, as its ciphertext blob.
        [HttpGet("{id:int}/history/{historyId:int}")]
        public async Task<IActionResult> GetHistoryEntry(int id, int historyId, CancellationToken ct)
        {
            var result = await _entries.GetHistoryEntryAsync(CurrentUserId, id, historyId, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
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
