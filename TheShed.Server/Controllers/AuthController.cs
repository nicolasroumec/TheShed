using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TheShed.Server.Security;
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

        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
        {
            var result = await _auth.RegisterAsync(request, ct);
            if (!result.Success)
            {
                return Conflict(new { message = "El email ya está registrado." });
            }
            SetAuthCookie(result);
            return CreatedAtAction(nameof(Register), result.Response);
        }

        [EnableRateLimiting(RateLimitPolicies.Auth)]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        {
            var result = await _auth.LoginAsync(request, ct);
            if (!result.Success)
            {
                return Unauthorized(new { message = "Credenciales inválidas." });
            }
            SetAuthCookie(result);
            return Ok(result.Response);
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var username = User.FindFirstValue("username");
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);
            var exp = long.Parse(User.FindFirstValue("exp")!);

            return Ok(new AuthResponse
            {
                Username = username!,
                Email = email!,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("authToken", new CookieOptions { Path = "/" });
            return Ok();
        }

        private void SetAuthCookie(AuthResult result)
        {
            Response.Cookies.Append("authToken", result.Token!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = result.Response!.ExpiresAt
            });
        }
    }
}
