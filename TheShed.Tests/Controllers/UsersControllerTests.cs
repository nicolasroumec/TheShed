using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Users;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class UsersControllerTests
    {
        private static UsersController CreateController(IUserService users) => new(users)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        [Fact]
        public async Task GetPublicKey_UsuarioExiste_Devuelve200ConLaKey()
        {
            var controller = CreateController(new FakeUserService
            {
                Result = new UserPublicKeyResponse { UserId = 1, PublicKey = "pem" }
            });

            var result = await controller.GetPublicKey("ana@test.com", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<UserPublicKeyResponse>(ok.Value);
            Assert.Equal("pem", body.PublicKey);
        }

        [Fact]
        public async Task GetPublicKey_UsuarioNoExiste_Devuelve404()
        {
            var controller = CreateController(new FakeUserService { Result = null });

            var result = await controller.GetPublicKey("nadie@test.com", CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetPublicKey_EmailVacio_Devuelve400()
        {
            var controller = CreateController(new FakeUserService());

            var result = await controller.GetPublicKey("", CancellationToken.None);

            Assert.IsType<BadRequestResult>(result);
        }

        // Fake del servicio: devuelve resultados preconfigurados sin tocar la DB.
        private sealed class FakeUserService : IUserService
        {
            public UserPublicKeyResponse? Result { get; set; }

            public Task<UserPublicKeyResponse?> GetPublicKeyAsync(string email, CancellationToken ct = default)
                => Task.FromResult(Result);
        }
    }
}
