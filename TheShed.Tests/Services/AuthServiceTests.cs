using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
            new(db, new FakePasswordHasher(), new FakeJwtTokenService(),
                Options.Create(new JwtSettings { Key = "unit-test-signing-key-at-least-32-bytes" }));

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
            Assert.False(string.IsNullOrEmpty(result.Token));

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

        [Fact]
        public async Task RegisterAsync_ConKeypair_LoDevuelveEnLaRespuesta()
        {
            using var db = CreateContext();
            var service = CreateService(db);

            var result = await service.RegisterAsync(new RegisterRequest
            {
                Username = "Ana",
                Email = "ana@test.com",
                Password = "Sup3rSecret!",
                PublicKey = "pem",
                EncryptedPrivateKey = "blob"
            });

            Assert.Equal("pem", result.Response!.PublicKey);
            Assert.Equal("blob", result.Response!.EncryptedPrivateKey);
        }

        // --- GetKeypairAsync ---

        [Fact]
        public async Task GetKeypairAsync_UsuarioExistente_DevuelvePublicKeyYPrivateKeyCifrada()
        {
            using var db = CreateContext();
            var service = CreateService(db);
            await service.RegisterAsync(new RegisterRequest
            {
                Username = "Ana",
                Email = "ana@test.com",
                Password = "Sup3rSecret!",
                PublicKey = "pem",
                EncryptedPrivateKey = "blob"
            });
            var userId = (await db.Users.SingleAsync()).Id;

            var (publicKey, encryptedPrivateKey) = await service.GetKeypairAsync(userId);

            Assert.Equal("pem", publicKey);
            Assert.Equal("blob", encryptedPrivateKey);
        }

        [Fact]
        public async Task GetKeypairAsync_UsuarioInexistente_DevuelveNulls()
        {
            using var db = CreateContext();
            var service = CreateService(db);

            var (publicKey, encryptedPrivateKey) = await service.GetKeypairAsync(999);

            Assert.Null(publicKey);
            Assert.Null(encryptedPrivateKey);
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

        // --- GetPreloginSaltAsync ---

        [Fact]
        public async Task GetPreloginSaltAsync_KnownEmail_ReturnsRealSalt()
        {
            using var db = CreateContext();
            db.Users.Add(new User { Username = "x", Email = "ana@test.com", PasswordHash = "h", KeySalt = "cmVhbC1zYWx0LTE2Ynl0ZQ==" });
            await db.SaveChangesAsync();

            var salt = await CreateService(db).GetPreloginSaltAsync(" ANA@test.com ");

            Assert.Equal("cmVhbC1zYWx0LTE2Ynl0ZQ==", salt);
        }

        [Fact]
        public async Task GetPreloginSaltAsync_UnknownEmail_ReturnsStableFakeSaltOfRealLength()
        {
            using var db = CreateContext();
            var service = CreateService(db);

            var first = await service.GetPreloginSaltAsync("nobody@test.com");
            var again = await service.GetPreloginSaltAsync("NOBODY@test.com");
            var other = await service.GetPreloginSaltAsync("someone-else@test.com");

            // Stable, or a second call would give away that the account doesn't exist.
            Assert.Equal(first, again);
            Assert.NotEqual(first, other);
            Assert.Equal(16, Convert.FromBase64String(first).Length);
        }

        // --- ChangePasswordAsync ---

        private static ChangePasswordRequest SampleChange(string current) => new()
        {
            CurrentPassword = current,
            NewPassword = "N3wSecret!",
            NewKeySalt = "bmV3LXNhbHQ=",
            NewEncryptedPrivateKey = "new-blob"
        };

        [Fact]
        public async Task ChangePasswordAsync_CorrectCurrent_SwapsHashSaltAndPrivateKey()
        {
            using var db = CreateContext();
            var service = CreateService(db);
            await service.RegisterAsync(new RegisterRequest
            {
                Username = "Ana", Email = "ana@test.com", Password = "Sup3rSecret!",
                KeySalt = "b2xkLXNhbHQ=", PublicKey = "pem", EncryptedPrivateKey = "old-blob"
            });
            var userId = (await db.Users.SingleAsync()).Id;

            var result = await service.ChangePasswordAsync(userId, SampleChange("Sup3rSecret!"));

            Assert.True(result.Success);
            Assert.Equal("bmV3LXNhbHQ=", result.Response!.KeySalt);
            Assert.Equal("new-blob", result.Response.EncryptedPrivateKey);
            Assert.Equal("pem", result.Response.PublicKey); // keypair itself is untouched
            Assert.False((await service.LoginAsync(new LoginRequest { Email = "ana@test.com", Password = "Sup3rSecret!" })).Success);
            Assert.True((await service.LoginAsync(new LoginRequest { Email = "ana@test.com", Password = "N3wSecret!" })).Success);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrent_ChangesNothing()
        {
            using var db = CreateContext();
            var service = CreateService(db);
            await service.RegisterAsync(new RegisterRequest
            {
                Username = "Ana", Email = "ana@test.com", Password = "Sup3rSecret!",
                KeySalt = "b2xkLXNhbHQ=", EncryptedPrivateKey = "old-blob"
            });
            var user = await db.Users.SingleAsync();

            var result = await service.ChangePasswordAsync(user.Id, SampleChange("wrong"));

            Assert.False(result.Success);
            Assert.Equal(AuthError.InvalidCredentials, result.Error);
            Assert.Equal("b2xkLXNhbHQ=", user.KeySalt);
            Assert.Equal("old-blob", user.EncryptedPrivateKey);
            Assert.Equal(FakePasswordHasher.Hashed("Sup3rSecret!"), user.PasswordHash);
        }

        /// <summary>Ana owns vault 1 (active) and vault 2 (trashed); Bob owns vault 3, shared with
        /// Ana (her wrap there is RSA, not under her stretched key). Returns Ana's id.</summary>
        private static async Task<int> SeedVaultsAsync(TheShedContext db, AuthService service)
        {
            await service.RegisterAsync(new RegisterRequest
            {
                Username = "Ana", Email = "ana@test.com", Password = "Sup3rSecret!",
                KeySalt = "b2xkLXNhbHQ=", EncryptedPrivateKey = "old-blob"
            });
            var ana = await db.Users.SingleAsync();
            var bob = new User { Username = "Bob", Email = "bob@test.com", PasswordHash = "h" };
            db.Users.Add(bob);
            await db.SaveChangesAsync();

            db.Vaults.AddRange(
                new Vault { Id = 1, OwnerId = ana.Id, Name = "Mine" },
                new Vault { Id = 2, OwnerId = ana.Id, Name = "Trashed", IsDeleted = true },
                new Vault { Id = 3, OwnerId = bob.Id, Name = "Bob's" });
            db.VaultKeyWraps.AddRange(
                new VaultKeyWrap { VaultId = 1, UserId = ana.Id, WrappedKey = "ana-1" },
                new VaultKeyWrap { VaultId = 2, UserId = ana.Id, WrappedKey = "ana-2" },
                new VaultKeyWrap { VaultId = 3, UserId = bob.Id, WrappedKey = "bob-3" },
                new VaultKeyWrap { VaultId = 3, UserId = ana.Id, WrappedKey = "ana-3-rsa" });
            await db.SaveChangesAsync();
            return ana.Id;
        }

        [Fact]
        public async Task GetOwnedVaultKeysAsync_ReturnsOwnWrapsOnOwnedVaults_IncludingTrashed()
        {
            using var db = CreateContext();
            var service = CreateService(db);
            var anaId = await SeedVaultsAsync(db, service);

            var keys = await service.GetOwnedVaultKeysAsync(anaId);

            // Trashed vault 2 counts: it can be restored, and would come back unopenable.
            Assert.Equal(["ana-1", "ana-2"], keys.OrderBy(k => k.VaultId).Select(k => k.WrappedKey));
        }

        [Fact]
        public async Task ChangePasswordAsync_RewrapsEveryOwnedVaultKey_AndNothingElse()
        {
            using var db = CreateContext();
            var service = CreateService(db);
            var anaId = await SeedVaultsAsync(db, service);
            var request = SampleChange("Sup3rSecret!");
            request.VaultKeys = [new() { VaultId = 1, WrappedKey = "new-1" }, new() { VaultId = 2, WrappedKey = "new-2" }];

            var result = await service.ChangePasswordAsync(anaId, request);

            Assert.True(result.Success);
            var wraps = await db.VaultKeyWraps.IgnoreQueryFilters().OrderBy(w => w.Id).Select(w => w.WrappedKey).ToListAsync();
            Assert.Equal(["new-1", "new-2", "bob-3", "ana-3-rsa"], wraps);
        }

        [Theory]
        [InlineData(new[] { 1 })]          // trashed vault 2 missing
        [InlineData(new[] { 1, 2, 3 })]    // vault 3 isn't Ana's
        [InlineData(new[] { 1, 1, 2 })]    // duplicate
        public async Task ChangePasswordAsync_VaultKeySetMismatch_ChangesNothing(int[] vaultIds)
        {
            using var db = CreateContext();
            var service = CreateService(db);
            var anaId = await SeedVaultsAsync(db, service);
            var request = SampleChange("Sup3rSecret!");
            request.VaultKeys = vaultIds.Select(id => new OwnedVaultKey { VaultId = id, WrappedKey = "new" }).ToList();

            var result = await service.ChangePasswordAsync(anaId, request);

            Assert.False(result.Success);
            Assert.Equal(AuthError.VaultKeysOutOfDate, result.Error);
            Assert.Equal("b2xkLXNhbHQ=", (await db.Users.FindAsync(anaId))!.KeySalt);
            Assert.DoesNotContain("new", await db.VaultKeyWraps.IgnoreQueryFilters().Select(w => w.WrappedKey).ToListAsync());
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
