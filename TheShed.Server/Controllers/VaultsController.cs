using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Vaults;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class VaultsController : ControllerBase
    {
        private readonly IVaultService _vaults;

        public VaultsController(IVaultService vaults) => _vaults = vaults;

        // GET /api/vaults — vaults the caller owns or is a member of.
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var items = await _vaults.ListAsync(CurrentUserId, ct);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var result = await _vaults.GetAsync(CurrentUserId, id, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpPost]
        public async Task<IActionResult> Create(VaultCreateRequest request, CancellationToken ct)
        {
            var vault = await _vaults.CreateAsync(CurrentUserId, request, ct);
            return CreatedAtAction(nameof(Get), new { id = vault.Id }, vault);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, VaultUpdateRequest request, CancellationToken ct)
        {
            var result = await _vaults.UpdateAsync(CurrentUserId, id, request, ct);
            return result.Success ? Ok(result.Value) : MapError(result.Error);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _vaults.DeleteAsync(CurrentUserId, id, ct);
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
