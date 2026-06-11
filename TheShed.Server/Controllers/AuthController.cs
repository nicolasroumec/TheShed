using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
        {
            var result = await _auth.RegisterAsync(request, ct);
            if (!result.Success)
            {
                return Conflict(new { message = "El email ya está registrado." });
            }
            return CreatedAtAction(nameof(Register), result.Response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        {
            var result = await _auth.LoginAsync(request, ct);
            if (!result.Success)
            {
                return Unauthorized(new { message = "Credenciales inválidas." });
            }
            return Ok(result.Response);
        }
    }
}
