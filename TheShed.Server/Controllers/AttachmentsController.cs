using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Enums;
using TheShed.Server.Services;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/entries/{entryId:int}/attachments")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachments;

        public AttachmentsController(IAttachmentService attachments) => _attachments = attachments;

        [HttpGet]
        public async Task<IActionResult> List(int entryId, CancellationToken ct)
        {
            var result = await _attachments.ListAsync(CurrentUserId, entryId, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(int entryId, IFormFile file, CancellationToken ct)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);

            var result = await _attachments.UploadAsync(CurrentUserId, entryId, file.FileName, stream.ToArray(), ct);
            if (!result.Success)
            {
                return MapError(result.Error);
            }
            return CreatedAtAction(nameof(List), new { entryId }, result.Value);
        }

        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> Download(int entryId, int id, CancellationToken ct)
        {
            var result = await _attachments.DownloadAsync(CurrentUserId, entryId, id, ct);
            if (!result.Success)
            {
                return MapError(result.Error);
            }
            return File(result.Value!.Content, "application/octet-stream", result.Value.FileName);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int entryId, int id, CancellationToken ct)
        {
            var result = await _attachments.DeleteAsync(CurrentUserId, entryId, id, ct);
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
            EntryError.InvalidFile => BadRequest("File is empty, too large, or not an allowed type (.pdf, .jpg, .jpeg, .png, .txt)."),
            _ => Problem()
        };
    }
}
