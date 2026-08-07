using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Auth
{
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _http;
        private static readonly AuthenticationState Anonymous =
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        public JwtAuthenticationStateProvider(HttpClient http) => _http = http;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var me = await _http.GetFromJsonAsync<AuthResponse>("api/auth/me");
                return me is null ? Anonymous : new AuthenticationState(new ClaimsPrincipal(ToIdentity(me)));
            }
            catch (HttpRequestException)
            {
                // Not authenticated (401): no cookie, or it expired.
                return Anonymous;
            }
        }

        /// <summary>Called after a successful login/register to refresh the UI — the browser
        /// already stored the cookie from the Set-Cookie response header.</summary>
        public async Task RefreshAsync()
        {
            var state = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }

        /// <summary>Called after logout to clear the UI.</summary>
        public void NotifyLoggedOut() =>
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));

        private static ClaimsIdentity ToIdentity(AuthResponse me) => new(new[]
        {
            new Claim("username", me.Username),
            new Claim("email", me.Email)
        }, authenticationType: "jwt");
    }
}
