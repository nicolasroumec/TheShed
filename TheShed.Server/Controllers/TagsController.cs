using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Tags;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tags;

        public TagsController(ITagService tags) => _tags = tags;

        // GET /api/tags — the caller's own tags.
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var result = await _tags.ListAsync(CurrentUserId, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TagCreateRequest request, CancellationToken ct)
        {
            var result = await _tags.CreateAsync(CurrentUserId, request, ct);
            if (!result.Success)
            {
                return MapError(result.Error);
            }
            return CreatedAtAction(nameof(List), new { id = result.Value!.Id }, result.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, TagUpdateRequest request, CancellationToken ct)
        {
            var result = await _tags.UpdateAsync(CurrentUserId, id, request, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _tags.DeleteAsync(CurrentUserId, id, ct);
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
            EntryError.Conflict => Conflict("You already have a tag with that name."),
            _ => Problem()
        };
    }
}
