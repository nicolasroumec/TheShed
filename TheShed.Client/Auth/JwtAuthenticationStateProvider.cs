using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TheShed.Client.Services;

namespace TheShed.Client.Auth
{
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        public const string TokenKey = "authToken";

        private readonly ILocalStorageService _storage;
        private readonly HttpClient _http;
        private static readonly AuthenticationState Anonymous =
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        public JwtAuthenticationStateProvider(ILocalStorageService storage, HttpClient http)
        {
            _storage = storage;
            _http = http;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _storage.GetItemAsync(TokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                return Anonymous;
            }

            var expiry = JwtParser.GetExpiry(token);
            if (expiry is not null && expiry <= DateTime.UtcNow)
            {
                // Expired token: clear it and stay anonymous.
                await _storage.RemoveItemAsync(TokenKey);
                _http.DefaultRequestHeaders.Authorization = null;
                return Anonymous;
            }

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var identity = new ClaimsIdentity(JwtParser.ParseClaims(token), authenticationType: "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        /// <summary>Called after a successful login to refresh the UI.</summary>
        public void NotifyAuthenticated(string token)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var identity = new ClaimsIdentity(JwtParser.ParseClaims(token), authenticationType: "jwt");
            var state = new AuthenticationState(new ClaimsPrincipal(identity));
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }

        /// <summary>Called after logout to clear the UI.</summary>
        public void NotifyLoggedOut()
        {
            _http.DefaultRequestHeaders.Authorization = null;
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }
    }
}
