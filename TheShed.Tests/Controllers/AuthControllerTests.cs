using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Auth;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class AuthControllerTests
    {
        private static AuthResponse SampleResponse() => new()
        {
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Username = "ana",
            Email = "ana@test.com"
        };

        private static AuthController CreateController(IAuthService auth) => new(auth)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        [Fact]
        public async Task Register_Exito_Devuelve201()
        {
            var controller = CreateController(new FakeAuthService
            {
                RegisterResult = new AuthResult(true, AuthError.None, SampleResponse(), "token")
            });

            var result = await controller.Register(new RegisterRequest(), CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.IsType<AuthResponse>(created.Value);
        }

        [Fact]
        public async Task Register_Exito_SeteaCookieAuthToken()
        {
            var controller = CreateController(new FakeAuthService
            {
                RegisterResult = new AuthResult(true, AuthError.None, SampleResponse(), "token")
            });

            await controller.Register(new RegisterRequest(), CancellationToken.None);

            var setCookie = controller.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
            Assert.Contains("authToken=token", setCookie);
            Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Register_EmailEnUso_Devuelve409()
        {
            var controller = CreateController(new FakeAuthService
            {
                RegisterResult = new AuthResult(false, AuthError.EmailInUse, null)
            });

            var result = await controller.Register(new RegisterRequest(), CancellationToken.None);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task Login_Exito_Devuelve200()
        {
            var controller = CreateController(new FakeAuthService
            {
                LoginResult = new AuthResult(true, AuthError.None, SampleResponse(), "token")
            });

            var result = await controller.Login(new LoginRequest(), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            Assert.IsType<AuthResponse>(ok.Value);
        }

        [Fact]
        public async Task Login_Exito_SeteaCookieAuthToken()
        {
            var controller = CreateController(new FakeAuthService
            {
                LoginResult = new AuthResult(true, AuthError.None, SampleResponse(), "token")
            });

            await controller.Login(new LoginRequest(), CancellationToken.None);

            var setCookie = controller.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
            Assert.Contains("authToken=token", setCookie);
            Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Login_CredencialesInvalidas_Devuelve401()
        {
            var controller = CreateController(new FakeAuthService
            {
                LoginResult = new AuthResult(false, AuthError.InvalidCredentials, null)
            });

            var result = await controller.Login(new LoginRequest(), CancellationToken.None);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        [Fact]
        public void Me_Devuelve200ConLosClaims()
        {
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("username", "ana"),
                new Claim(ClaimTypes.Email, "ana@test.com"),
                new Claim("exp", expiresAt.ToUnixTimeSeconds().ToString())
            }, "test");
            var controller = CreateController(new FakeAuthService());
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            var result = controller.Me();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AuthResponse>(ok.Value);
            Assert.Equal("ana", response.Username);
            Assert.Equal("ana@test.com", response.Email);
            Assert.Equal(expiresAt.UtcDateTime, response.ExpiresAt, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Logout_BorraCookieAuthToken()
        {
            var controller = CreateController(new FakeAuthService());

            var result = controller.Logout();

            Assert.IsType<OkResult>(result);
            var setCookie = controller.ControllerContext.HttpContext.Response.Headers.SetCookie.ToString();
            Assert.Contains("authToken=", setCookie);
            Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.OrdinalIgnoreCase);
        }

        // Fake del servicio: devuelve resultados preconfigurados sin tocar la DB.
        private sealed class FakeAuthService : IAuthService
        {
            public AuthResult RegisterResult { get; set; } = default!;
            public AuthResult LoginResult { get; set; } = default!;

            public Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
                => Task.FromResult(RegisterResult);

            public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
                => Task.FromResult(LoginResult);
        }
    }
}
