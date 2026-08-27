using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Services;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;

        public UsersController(IUserService users) => _users = users;

        // GET /api/users/public-key?email= — the RSA public key for a user, used to wrap a
        // vault key for them when sharing a vault (Sprint 27). This is a read-only counterpart
        // to the email-enumeration surface VaultsController.AddMember already has (it tells the
        // caller "no user with that email" vs "already a member"), so it adds no new leak.
        [HttpGet("public-key")]
        public async Task<IActionResult> GetPublicKey([FromQuery] string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest();
            }

            var result = await _users.GetPublicKeyAsync(email, ct);
            return result is null ? NotFound() : Ok(result);
        }
    }
}
