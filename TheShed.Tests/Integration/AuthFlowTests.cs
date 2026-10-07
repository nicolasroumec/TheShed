using System.Net;
using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Auth;
using TheShed.Shared.Models.DTOs.Vaults;
using Xunit;

namespace TheShed.Tests.Integration
{
    /// <summary>Register/login/me against the real ASP.NET Core pipeline, plus antiforgery
    /// (Sprint 32) exercised as an actual HTTP round trip instead of AntiforgeryControllerTests'
    /// in-process IAntiforgery call. Duplicate-email (409) and authenticated /me (200) are
    /// already covered by AuthTestHelperTests — not repeated here.</summary>
    public class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Register_Exito_Devuelve201YSeteaLaCookieAuthToken()
        {
            var client = _factory.CreateAuthenticatedClient();

            var (email, username) = await client.RegisterNewUserAsync();

            // Registered through RegisterNewUserAsync above; the assertion here is specifically
            // that the server actually issued the session cookie, not just that the call
            // succeeded — a 201 with no cookie would still leave the user logged out.
            var me = await client.GetAsync("api/auth/me");
            me.EnsureSuccessStatusCode();
            var body = await me.Content.ReadAsStringAsync();
            Assert.Contains(username, body);
            Assert.Contains(email, body);
        }

        [Fact]
        public async Task Register_SinMaterialCriptografico_Devuelve400()
        {
            var client = _factory.CreateAuthenticatedClient();

            // KeySalt/PublicKey/EncryptedPrivateKey are generated client-side and not
            // [Required] (RegisterRequest), so AuthController itself must reject a caller that
            // skips the client entirely — this is what proves that guard actually runs.
            var response = await client.PostJsonWithCsrfAsync("api/auth/register", new
            {
                Username = "sincripto",
                Email = "sincripto@example.com",
                Password = "IntegrationTest123!"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_CredencialesValidas_Devuelve200()
        {
            var client = _factory.CreateAuthenticatedClient();
            var (email, _) = await client.RegisterNewUserAsync(password: "IntegrationTest123!");

            var loginClient = _factory.CreateAuthenticatedClient();
            var response = await loginClient.PostJsonWithCsrfAsync("api/auth/login", new
            {
                Email = email,
                Password = "IntegrationTest123!"
            });

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Prelogin_IsAnonymous_ReturnsRealSaltOrFakeOne()
        {
            var client = _factory.CreateAuthenticatedClient();
            var (email, _) = await client.RegisterNewUserAsync();

            var anonymous = _factory.CreateAuthenticatedClient();
            var known = await anonymous.GetFromJsonAsync<PreloginResponse>($"api/auth/prelogin?email={Uri.EscapeDataString(email)}");
            var unknown = await anonymous.GetFromJsonAsync<PreloginResponse>("api/auth/prelogin?email=nobody%40example.com");

            Assert.Equal("c2FsdA==", known!.KeySalt); // the salt AuthTestHelper registers with
            Assert.Equal(16, Convert.FromBase64String(unknown!.KeySalt).Length);
        }

        [Fact]
        public async Task ChangePassword_RoundTrip_NewPasswordLogsInAndOldOneDoesNot()
        {
            var client = _factory.CreateAuthenticatedClient();
            var (email, _) = await client.RegisterNewUserAsync(password: "IntegrationTest123!");
            var otherDevice = _factory.CreateAuthenticatedClient();
            (await otherDevice.PostJsonWithCsrfAsync("api/auth/login", new { Email = email, Password = "IntegrationTest123!" })).EnsureSuccessStatusCode();

            var wrong = await client.PostJsonWithCsrfAsync("api/auth/change-password", new ChangePasswordRequest
            {
                CurrentPassword = "not-it", NewPassword = "Changed123!",
                NewKeySalt = "bmV3LXNhbHQ=", NewEncryptedPrivateKey = "new-blob"
            });
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

            // An owned vault: its key is wrapped under the master key, so it has to move too.
            var created = await client.PostJsonWithCsrfAsync("api/vaults", new VaultCreateRequest { Name = "Mine", VaultKeyWrap = "old-wrap" });
            var vault = (await created.Content.ReadFromJsonAsync<VaultResponse>())!;
            var owned = await client.GetFromJsonAsync<List<OwnedVaultKey>>("api/auth/owned-vault-keys");
            Assert.Equal("old-wrap", Assert.Single(owned!).WrappedKey);

            var missingVault = await client.PostJsonWithCsrfAsync("api/auth/change-password", new ChangePasswordRequest
            {
                CurrentPassword = "IntegrationTest123!", NewPassword = "Changed123!",
                NewKeySalt = "bmV3LXNhbHQ=", NewEncryptedPrivateKey = "new-blob"
            });
            Assert.Equal(HttpStatusCode.Conflict, missingVault.StatusCode);

            var ok = await client.PostJsonWithCsrfAsync("api/auth/change-password", new ChangePasswordRequest
            {
                CurrentPassword = "IntegrationTest123!", NewPassword = "Changed123!",
                NewKeySalt = "bmV3LXNhbHQ=", NewEncryptedPrivateKey = "new-blob",
                VaultKeys = [new() { VaultId = vault.Id, WrappedKey = "new-wrap" }]
            });
            ok.EnsureSuccessStatusCode();
            Assert.Equal("new-wrap", (await client.GetFromJsonAsync<VaultResponse>($"api/vaults/{vault.Id}"))!.WrappedKey);

            // The re-issued cookie must carry the new keySalt claim, or unlock breaks on F5.
            var me = await client.GetFromJsonAsync<AuthResponse>("api/auth/me");
            Assert.Equal("bmV3LXNhbHQ=", me!.KeySalt);
            Assert.Equal("new-blob", me.EncryptedPrivateKey);

            // Every other session is revoked (N3): it would carry the stale keySalt claim.
            Assert.Equal(HttpStatusCode.Unauthorized, (await otherDevice.GetAsync("api/auth/me")).StatusCode);

            var fresh = _factory.CreateAuthenticatedClient();
            var oldLogin = await fresh.PostJsonWithCsrfAsync("api/auth/login", new { Email = email, Password = "IntegrationTest123!" });
            var newLogin = await fresh.PostJsonWithCsrfAsync("api/auth/login", new { Email = email, Password = "Changed123!" });
            Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
            newLogin.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task LogoutAll_RevokesEverySessionIncludingTheCallers()
        {
            var client = _factory.CreateAuthenticatedClient();
            var (email, _) = await client.RegisterNewUserAsync(password: "IntegrationTest123!");
            var otherDevice = _factory.CreateAuthenticatedClient();
            (await otherDevice.PostJsonWithCsrfAsync("api/auth/login", new { Email = email, Password = "IntegrationTest123!" })).EnsureSuccessStatusCode();

            // otherDevice stands in for a stolen cookie: plain logout only deletes the cookie on
            // the device that calls it, so this is the case logout-all exists for.
            (await otherDevice.GetAsync("api/auth/me")).EnsureSuccessStatusCode();

            (await client.PostJsonWithCsrfAsync("api/auth/logout-all", new { })).EnsureSuccessStatusCode();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await otherDevice.GetAsync("api/auth/me")).StatusCode);

            // Revocation isn't a lockout: a fresh login gets a token with the new version.
            var relogin = _factory.CreateAuthenticatedClient();
            (await relogin.PostJsonWithCsrfAsync("api/auth/login", new { Email = email, Password = "IntegrationTest123!" })).EnsureSuccessStatusCode();
            (await relogin.GetAsync("api/auth/me")).EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ChangePassword_Anonymous_Returns401()
        {
            var client = _factory.CreateAuthenticatedClient();

            var response = await client.PostJsonWithCsrfAsync("api/auth/change-password", new ChangePasswordRequest
            {
                CurrentPassword = "x", NewPassword = "Changed123!", NewKeySalt = "c2FsdA==", NewEncryptedPrivateKey = "b"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_CredencialesInvalidas_Devuelve401()
        {
            var client = _factory.CreateAuthenticatedClient();

            var response = await client.PostJsonWithCsrfAsync("api/auth/login", new
            {
                Email = "no-existe@example.com",
                Password = "loquesea"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Mutacion_SinHeaderCsrf_Devuelve400()
        {
            var client = _factory.CreateAuthenticatedClient();

            // Raw PostAsJsonAsync, deliberately bypassing AuthTestHelper — no X-CSRF-TOKEN
            // header at all, the case a browser can't even reach since CsrfHandler (client
            // Program.cs) attaches it to every mutating request automatically.
            var response = await client.PostAsJsonAsync("api/auth/login", new
            {
                Email = "whoever@example.com",
                Password = "loquesea"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Mutacion_ConTokenQueNoMatcheaLaCookie_Devuelve400()
        {
            var client = _factory.CreateAuthenticatedClient();
            // Fetching a token stores the pairing cookie on the client — the header below
            // deliberately isn't that token, so the pair no longer matches.
            await client.FetchCsrfTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
            {
                Content = JsonContent.Create(new { Email = "whoever@example.com", Password = "loquesea" })
            };
            request.Headers.Add("X-CSRF-TOKEN", "not-the-token-that-was-issued");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
