using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Tests.Integration
{
    /// <summary>Stands in for the browser's CsrfHandler + register form: fetches a CSRF token,
    /// attaches it to mutating requests, and registers a user with unique data. The client's
    /// authToken cookie (set by Register/Login) is tracked automatically — CreateClient()
    /// defaults to HandleCookies = true, same as a real browser tab.</summary>
    internal static class AuthTestHelper
    {
        /// <summary>Fetches a fresh CSRF token. Not cached — the token is bound to the caller's
        /// identity at the moment it's issued (see DECISIONS.md D10), so a stale one silently
        /// stops validating the instant a test logs in; fetching fresh before every mutation
        /// sidesteps that entirely instead of replicating CsrfHandler's Invalidate() dance.</summary>
        public static async Task<string> FetchCsrfTokenAsync(this HttpClient client)
        {
            var response = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("api/antiforgery/token");
            return response!.Token;
        }

        public static Task<HttpResponseMessage> PostJsonWithCsrfAsync<T>(this HttpClient client, string url, T body) =>
            client.SendWithCsrfAsync(HttpMethod.Post, url, body);

        public static Task<HttpResponseMessage> PutJsonWithCsrfAsync<T>(this HttpClient client, string url, T body) =>
            client.SendWithCsrfAsync(HttpMethod.Put, url, body);

        public static async Task<HttpResponseMessage> DeleteWithCsrfAsync(this HttpClient client, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Add("X-CSRF-TOKEN", await client.FetchCsrfTokenAsync());
            return await client.SendAsync(request);
        }

        private static async Task<HttpResponseMessage> SendWithCsrfAsync<T>(this HttpClient client, HttpMethod method, string url, T body)
        {
            var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
            request.Headers.Add("X-CSRF-TOKEN", await client.FetchCsrfTokenAsync());
            return await client.SendAsync(request);
        }

        /// <summary>Registers a brand-new user (unique email per call, so tests never need to
        /// reset the database between them) and leaves the client authenticated — the same
        /// authToken cookie a real browser would carry after Register.razor's submit.</summary>
        public static async Task<(string Email, string Username)> RegisterNewUserAsync(
            this HttpClient client, string password = "IntegrationTest123!")
        {
            var unique = Guid.NewGuid().ToString("N")[..12];
            var request = new RegisterRequest
            {
                Username = "it" + unique,
                Email = $"it-{unique}@example.com",
                Password = password,
                KeySalt = "c2FsdA==",
                PublicKey = "pem",
                EncryptedPrivateKey = "blob"
            };

            var response = await client.PostJsonWithCsrfAsync("api/auth/register", request);
            response.EnsureSuccessStatusCode();

            return (request.Email, request.Username);
        }
    }
}
