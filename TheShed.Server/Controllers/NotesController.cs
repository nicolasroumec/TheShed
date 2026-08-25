using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Notes;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly ISecureNoteService _notes;

        public NotesController(ISecureNoteService notes) => _notes = notes;

        // GET /api/notes?vaultId=123 — metadata only, no content.
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int vaultId, CancellationToken ct)
        {
            var result = await _notes.ListAsync(CurrentUserId, vaultId, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        // GET /api/notes/123 — single note, content as its ciphertext blob (Sprint 26).
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var result = await _notes.GetAsync(CurrentUserId, id, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpPost]
        public async Task<IActionResult> Create(NoteCreateRequest request, CancellationToken ct)
        {
            var result = await _notes.CreateAsync(CurrentUserId, request, ct);
            if (!result.Success)
            {
                return MapError(result.Error);
            }
            return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, NoteUpdateRequest request, CancellationToken ct)
        {
            var result = await _notes.UpdateAsync(CurrentUserId, id, request, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _notes.DeleteAsync(CurrentUserId, id, ct);
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
