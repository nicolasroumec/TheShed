using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using TheShed.Client.Auth;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Services
{
    /// <summary>
    /// Client-side auth orchestration: calls the API, persists the JWT and refreshes
    /// the authentication state.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _storage;
        private readonly JwtAuthenticationStateProvider _stateProvider;

        public AuthService(
            HttpClient http,
            ILocalStorageService storage,
            AuthenticationStateProvider stateProvider)
        {
            _http = http;
            _storage = storage;
            _stateProvider = (JwtAuthenticationStateProvider)stateProvider;
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AuthResult.Fail(await ReadErrorAsync(response, "Invalid credentials."));
            }
            return await HandleSuccessAsync(response);
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return AuthResult.Fail(await ReadErrorAsync(response, "The email is already registered."));
            }
            return await HandleSuccessAsync(response);
        }

        public async Task LogoutAsync()
        {
            await _storage.RemoveItemAsync(JwtAuthenticationStateProvider.TokenKey);
            _stateProvider.NotifyLoggedOut();
        }

        private async Task<AuthResult> HandleSuccessAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return AuthResult.Fail(await ReadErrorAsync(response, "Unexpected error. Please try again."));
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
            {
                return AuthResult.Fail("Unexpected response from the server.");
            }

            await _storage.SetItemAsync(JwtAuthenticationStateProvider.TokenKey, auth.Token);
            _stateProvider.NotifyAuthenticated(auth.Token);
            return AuthResult.Ok(auth);
        }

        private static async Task<string> ReadErrorAsync(HttpResponseMessage response, string fallback)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var message))
                    {
                        return message.GetString() ?? fallback;
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON body: fall through to the default message.
            }
            return fallback;
        }
    }
}
