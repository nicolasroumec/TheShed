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
            Token = "token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Username = "ana",
            Email = "ana@test.com"
        };

        [Fact]
        public async Task Register_Exito_Devuelve201()
        {
            var controller = new AuthController(new FakeAuthService
            {
                RegisterResult = new AuthResult(true, AuthError.None, SampleResponse())
            });

            var result = await controller.Register(new RegisterRequest(), CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.IsType<AuthResponse>(created.Value);
        }

        [Fact]
        public async Task Register_EmailEnUso_Devuelve409()
        {
            var controller = new AuthController(new FakeAuthService
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
            var controller = new AuthController(new FakeAuthService
            {
                LoginResult = new AuthResult(true, AuthError.None, SampleResponse())
            });

            var result = await controller.Login(new LoginRequest(), CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            Assert.IsType<AuthResponse>(ok.Value);
        }

        [Fact]
        public async Task Login_CredencialesInvalidas_Devuelve401()
        {
            var controller = new AuthController(new FakeAuthService
            {
                LoginResult = new AuthResult(false, AuthError.InvalidCredentials, null)
            });

            var result = await controller.Login(new LoginRequest(), CancellationToken.None);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
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
