using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Server.Controllers
{
    /// <summary>Issues the CSRF token (A4). Anonymous on purpose: the client fetches it once at
    /// startup, before login, so the same token also covers the Login/Register mutations.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AntiforgeryController : ControllerBase
    {
        private readonly IAntiforgery _antiforgery;

        public AntiforgeryController(IAntiforgery antiforgery) => _antiforgery = antiforgery;

        [HttpGet("token")]
        public IActionResult GetToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new AntiforgeryTokenResponse(tokens.RequestToken!));
        }
    }
}
