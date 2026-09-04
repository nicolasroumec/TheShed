using System.Net;
using System.Net.Http.Json;
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
