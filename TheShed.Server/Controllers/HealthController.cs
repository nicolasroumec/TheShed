using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Services;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IPasswordHealthService _health;

        public HealthController(IPasswordHealthService health) => _health = health;

        // GET /api/health/passwords — strength + reuse verdict per entry across the
        // caller's vaults. Never returns a plaintext password.
        [HttpGet("passwords")]
        public async Task<IActionResult> GetPasswordHealth(CancellationToken ct)
        {
            var report = await _health.GetReportAsync(CurrentUserId, ct);
            return Ok(report);
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub")
                      ?? throw new InvalidOperationException("Missing user id claim."));
    }
}
