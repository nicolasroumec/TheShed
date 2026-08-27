using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Services;
using TheShed.Shared.Models.Entities;
using Xunit;

namespace TheShed.Tests.Services
{
    public class UserServiceTests
    {
        // DbContext con provider InMemory y base única por test, para aislarlos.
        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        [Fact]
        public async Task GetPublicKeyAsync_UsuarioConKeypair_DevuelveIdYPublicKey()
        {
            using var db = CreateContext();
            db.Users.Add(new User { Username = "ana", Email = "ana@test.com", PasswordHash = "h", PublicKey = "pem" });
            await db.SaveChangesAsync();
            var service = new UserService(db);

            var result = await service.GetPublicKeyAsync("ana@test.com");

            Assert.NotNull(result);
            Assert.Equal("pem", result!.PublicKey);
        }

        [Fact]
        public async Task GetPublicKeyAsync_EmailInexistente_DevuelveNull()
        {
            using var db = CreateContext();
            var service = new UserService(db);

            var result = await service.GetPublicKeyAsync("nadie@test.com");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPublicKeyAsync_UsuarioSinKeypair_DevuelveNull()
        {
            using var db = CreateContext();
            // Pre-Sprint-25 account: no PublicKey yet.
            db.Users.Add(new User { Username = "vieja", Email = "vieja@test.com", PasswordHash = "h" });
            await db.SaveChangesAsync();
            var service = new UserService(db);

            var result = await service.GetPublicKeyAsync("vieja@test.com");

            Assert.Null(result);
        }
    }
}
