using System.Net;
using Microsoft.AspNetCore.Components;

namespace TheShed.Client.Services
{
    /// <summary>Sends the app to the login page when the server rejects the session (401) — the
    /// JWT expired or was revoked by "sign out everywhere" or a password change elsewhere (N3).
    /// Without it a revoked tab keeps showing stale data and failing calls until a reload.
    /// forceLoad restarts the WASM app, which wipes every in-memory key in one go instead of
    /// clearing each cache here (they're Scoped, this handler is a singleton).</summary>
    public class SessionExpiredHandler(NavigationManager nav) : DelegatingHandler
    {
        // A 401 from these is an answer, not an expired session: wrong credentials, and the
        // "am I logged in?" probe JwtAuthenticationStateProvider makes on every load.
        private static readonly string[] Exempt = ["api/auth/login", "api/auth/me"];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = await base.SendAsync(request, ct);
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (response.StatusCode == HttpStatusCode.Unauthorized && !Exempt.Any(path.EndsWith))
            {
                nav.NavigateTo("login", forceLoad: true);
            }
            return response;
        }
    }
}
