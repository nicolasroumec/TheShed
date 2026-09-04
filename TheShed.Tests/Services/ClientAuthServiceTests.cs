using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;
using TheShed.Client.Auth;
using TheShed.Client.Services;
using TheShed.Shared.Security;
using Xunit;

namespace TheShed.Tests.Services
{
    /// <summary>
    /// The client-side <see cref="TheShed.Client.Services.AuthService"/> (not the server one in
    /// <see cref="AuthServiceTests"/>): Sprint 30's session lock and unlock.
    /// </summary>
    public class ClientAuthServiceTests
    {
        private const string KeySalt = "c2FsdHktc2FsdC0xMjM0"; // any valid base64
        private const string PublicKey = "-----BEGIN PUBLIC KEY-----\nabc\n-----END PUBLIC KEY-----";
        private const string EncryptedPrivateKey = "d3JhcHBlZC1wcml2YXRlLWtleQ==";

        [Fact]
        public async Task UnlockAsync_WithTheCorrectPassword_RestoresTheSessionKeys()
        {
            var (auth, keyStore, ownKeypair, _) = Build(correctPassword: "hunter2");

            var result = await auth.UnlockAsync("hunter2");

            Assert.True(result.Success);
            Assert.Equal(FakeKdf.KeyFor("hunter2"), keyStore.Get());
            Assert.Equal((PublicKey, EncryptedPrivateKey), ownKeypair.Get());
        }

        [Fact]
        public async Task UnlockAsync_WithTheWrongPassword_LeavesTheSessionLocked()
        {
            var (auth, keyStore, ownKeypair, _) = Build(correctPassword: "hunter2");

            var result = await auth.UnlockAsync("not-the-password");

            Assert.False(result.Success);
            Assert.Equal("Incorrect master password.", result.Error);
            // Nothing partially restored: a failed unlock must leave no key behind.
            Assert.Null(keyStore.Get());
            Assert.Equal((null, null), ownKeypair.Get());
        }

        [Fact]
        public async Task UnlockAsync_WhenTheAccountHasNoKeysOnTheServer_FailsWithoutDeriving()
        {
            var (auth, keyStore, _, kdf) = Build(correctPassword: "hunter2", accountHasKeys: false);

            var result = await auth.UnlockAsync("hunter2");

            Assert.False(result.Success);
            Assert.Null(keyStore.Get());
            Assert.Equal(0, kdf.Derivations); // 600k PBKDF2 iterations not spent on a lost cause
        }

        [Fact]
        public async Task UnlockAsync_WhenTheCookieExpired_ReportsTheSessionIsGone()
        {
            var (auth, keyStore, _, _) = Build(correctPassword: "hunter2", meStatus: HttpStatusCode.Unauthorized);

            var result = await auth.UnlockAsync("hunter2");

            Assert.False(result.Success);
            Assert.Equal("Your session expired. Sign in again.", result.Error);
            Assert.Null(keyStore.Get());
        }

        /// <summary>What the idle timer calls (Sprint 30, Increment 2). Every decryption key has
        /// to go: leaving one behind keeps a vault readable after the session locked.</summary>
        [Fact]
        public void Lock_ClearsEveryDecryptionKeyTheSessionHolds()
        {
            var (auth, keyStore, ownKeypair, _) = Build(correctPassword: "hunter2");
            var vaultKeys = new VaultKeyCache();
            keyStore.Set(new byte[] { 1, 2, 3 });
            ownKeypair.Set(PublicKey, EncryptedPrivateKey);
            vaultKeys.Set(1, new byte[] { 4, 5, 6 });

            // Rebuilt around the same caches so Lock() acts on the ones asserted below.
            new AuthService(new HttpClient { BaseAddress = new Uri("https://localhost/") },
                new JwtAuthenticationStateProvider(new HttpClient { BaseAddress = new Uri("https://localhost/") }),
                new FakeKdf(), new ThrowingKeypairService(), keyStore, vaultKeys, ownKeypair,
                new FakeAesGcm("whatever"), new CsrfHandler(new FakeNavigationManager())).Lock();

            Assert.Null(keyStore.Get());
            Assert.Null(vaultKeys.Get(1));
            Assert.Equal((null, null), ownKeypair.Get());
        }

        private static (AuthService Auth, StretchedKeyStore KeyStore, OwnKeypairCache OwnKeypair, FakeKdf Kdf) Build(
            string correctPassword, bool accountHasKeys = true, HttpStatusCode meStatus = HttpStatusCode.OK)
        {
            var body = accountHasKeys
                ? $$"""
                    {"username":"nico","email":"nico@example.com","keySalt":"{{KeySalt}}",
                     "publicKey":"{{PublicKey.Replace("\n", "\\n")}}","encryptedPrivateKey":"{{EncryptedPrivateKey}}"}
                    """
                : """{"username":"nico","email":"nico@example.com"}""";

            var http = new HttpClient(new StubHandler(meStatus, body)) { BaseAddress = new Uri("https://localhost/") };
            var kdf = new FakeKdf();
            var keyStore = new StretchedKeyStore();
            var ownKeypair = new OwnKeypairCache();

            var auth = new AuthService(http, new JwtAuthenticationStateProvider(http), kdf,
                new ThrowingKeypairService(), keyStore, new VaultKeyCache(), ownKeypair,
                new FakeAesGcm(correctPassword), new CsrfHandler(new FakeNavigationManager()));

            return (auth, keyStore, ownKeypair, kdf);
        }

        private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
        }

        /// <summary>Deterministic stand-in for PBKDF2 — same password, same key.</summary>
        private sealed class FakeKdf : IKeyDerivationService
        {
            public int Derivations { get; private set; }

            public static byte[] KeyFor(string password) => SHA256.HashData(Encoding.UTF8.GetBytes(password));

            public byte[] GenerateSalt() => new byte[16];

            public Task<byte[]> DeriveKeyAsync(string password, byte[] salt)
            {
                Derivations++;
                return Task.FromResult(KeyFor(password));
            }
        }

        /// <summary>Stands in for Web Crypto: decrypting the wrapped private key only succeeds
        /// under the key the correct master password derives, exactly as the AES-GCM auth tag
        /// behaves in the browser.</summary>
        private sealed class FakeAesGcm(string correctPassword) : IAesGcmService
        {
            public Task<string> EncryptAsync(byte[] key, string plaintext) => Task.FromResult(plaintext);

            public Task<string> DecryptAsync(byte[] key, string ciphertext) =>
                key.SequenceEqual(FakeKdf.KeyFor(correctPassword))
                    ? Task.FromResult("private-key-pkcs8")
                    : throw new CryptographicException("auth tag mismatch");
        }

        private sealed class FakeNavigationManager : NavigationManager
        {
            public FakeNavigationManager() => Initialize("https://localhost/", "https://localhost/");
        }

        /// <summary>Unlock never generates or wraps a keypair; being called at all is the bug.</summary>
        private sealed class ThrowingKeypairService : IUserKeypairService
        {
            public Task<UserKeypair> GenerateAsync(byte[] stretchedMasterKey) => throw new InvalidOperationException();
            public Task<string> WrapKeyForMemberAsync(byte[] vaultKey, string memberPublicKeyPem) => throw new InvalidOperationException();
            public Task<byte[]> UnwrapKeyAsMemberAsync(byte[] stretchedMasterKey, string encryptedPrivateKey, string wrappedVaultKey) => throw new InvalidOperationException();
        }
    }
}
