using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Security;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Auth;
using TheShed.Shared.Models.Entities;
using Xunit;

namespace TheShed.Tests.Services
{
    public class AuthServiceTests
    {
        // DbContext con provider InMemory y base única por test, para aislarlos.
        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static AuthService CreateService(TheShedContext db) =>
            new(db, new FakePasswordHasher(), new FakeJwtTokenService());

        // --- Register ---

        [Fact]
        public async Task RegisterAsync_EmailNuevo_CreaUsuarioYDevuelveToken()
        {
            using var db = CreateContext();
            var service = CreateService(db);

            var result = await service.RegisterAsync(new RegisterRequest
            {
                Username = "Ana",
                Email = "Ana@Test.com",
                Password = "Sup3rSecret!"
            });

            Assert.True(result.Success);
            Assert.Equal(AuthError.None, result.Error);
            Assert.NotNull(result.Response);
            Assert.False(string.IsNullOrEmpty(result.Response!.Token));

            var user = await db.Users.SingleAsync();
            Assert.Equal("ana@test.com", user.Email);            // email normalizado a minúsculas
            Assert.Equal("Ana", user.Username);
            Assert.NotEqual("Sup3rSecret!", user.PasswordHash);  // nunca en texto plano
        }

        [Fact]
        public async Task RegisterAsync_EmailDuplicado_DevuelveEmailInUse()
        {
            using var db = CreateContext();
            db.Users.Add(new User { Username = "x", Email = "ana@test.com", PasswordHash = "h" });
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.RegisterAsync(new RegisterRequest
            {
                Username = "Otra",
                Email = "ANA@test.com",   // mismo email, distinta capitalización
                Password = "Sup3rSecret!"
            });

            Assert.False(result.Success);
            Assert.Equal(AuthError.EmailInUse, result.Error);
            Assert.Null(result.Response);
            Assert.Equal(1, await db.Users.CountAsync()); // no se creó un segundo usuario
        }

        // --- Login ---

        [Fact]
        public async Task LoginAsync_CredencialesValidas_DevuelveTokenYActualizaLastLogin()
        {
            using var db = CreateContext();
            var service = CreateService(db);
            await service.RegisterAsync(new RegisterRequest
            {
                Username = "Ana",
                Email = "ana@test.com",
                Password = "Sup3rSecret!"
            });

            var result = await service.LoginAsync(new LoginRequest
            {
                Email = "ANA@test.com",   // distinto casing, mismo usuario
                Password = "Sup3rSecret!"
            });

            Assert.True(result.Success);
            Assert.Equal("ana@test.com", result.Response!.Email);

            var user = await db.Users.SingleAsync();
            Assert.NotNull(user.LastLoginAt);
        }

        [Fact]
        public async Task LoginAsync_PasswordIncorrecta_DevuelveInvalidCredentials()
        {
            using var db = CreateContext();
            var service = CreateService(db);
            await service.RegisterAsync(new RegisterRequest
            {
                Username = "Ana",
                Email = "ana@test.com",
                Password = "Sup3rSecret!"
            });

            var result = await service.LoginAsync(new LoginRequest
            {
                Email = "ana@test.com",
                Password = "incorrecta"
            });

            Assert.False(result.Success);
            Assert.Equal(AuthError.InvalidCredentials, result.Error);
            Assert.Null(result.Response);
        }

        [Fact]
        public async Task LoginAsync_EmailInexistente_DevuelveInvalidCredentials()
        {
            using var db = CreateContext();
            var service = CreateService(db);

            var result = await service.LoginAsync(new LoginRequest
            {
                Email = "nadie@test.com",
                Password = "loquesea"
            });

            Assert.False(result.Success);
            Assert.Equal(AuthError.InvalidCredentials, result.Error);
        }

        [Fact]
        public async Task LoginAsync_UsuarioInactivo_DevuelveInvalidCredentials()
        {
            using var db = CreateContext();
            db.Users.Add(new User
            {
                Username = "x",
                Email = "inactivo@test.com",
                PasswordHash = FakePasswordHasher.Hashed("Sup3rSecret!"),
                IsActive = false
            });
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.LoginAsync(new LoginRequest
            {
                Email = "inactivo@test.com",
                Password = "Sup3rSecret!"
            });

            Assert.False(result.Success);
            Assert.Equal(AuthError.InvalidCredentials, result.Error);
        }

        // --- Fakes (sin Moq, para mantener el estilo liviano del repo) ---

        private sealed class FakePasswordHasher : IPasswordHasher
        {
            public static string Hashed(string password) => "hashed:" + password;
            public string Hash(string password) => Hashed(password);
            public bool Verify(string password, string hash) => hash == Hashed(password);
        }

        private sealed class FakeJwtTokenService : IJwtTokenService
        {
            public (string Token, DateTime ExpiresAt) GenerateToken(User user) =>
                ($"token-{user.Email}", DateTime.UtcNow.AddMinutes(60));
        }
    }
}
