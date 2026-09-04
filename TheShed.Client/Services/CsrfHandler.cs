using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Services
{
    /// <summary>Attaches the CSRF token (A4) to every mutating request. Fetches it once from
    /// `/api/antiforgery/token` and caches it for the browser session — the token stays valid as
    /// long as the antiforgery cookie the server paired it with does, so there's no need to
    /// re-fetch per request.</summary>
    public class CsrfHandler(NavigationManager nav) : DelegatingHandler
    {
        private string? _token;
        private readonly SemaphoreSlim _lock = new(1, 1);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (NeedsToken(request.Method))
            {
                request.Headers.Add("X-CSRF-TOKEN", await GetTokenAsync(ct));
            }
            return await base.SendAsync(request, ct);
        }

        private static bool NeedsToken(HttpMethod method) =>
            method != HttpMethod.Get && method != HttpMethod.Head &&
            method != HttpMethod.Options && method != HttpMethod.Trace;

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            if (_token is not null) return _token;
            await _lock.WaitAsync(ct);
            try
            {
                if (_token is not null) return _token;
                using var client = new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
                var result = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("api/antiforgery/token", ct);
                return _token = result!.Token;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
