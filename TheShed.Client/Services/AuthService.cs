using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using TheShed.Client.Auth;
using TheShed.Shared.Models.DTOs.Auth;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    /// <summary>
    /// Client-side auth orchestration: calls the API and refreshes the authentication state.
    /// The JWT itself lives in an HttpOnly cookie set by the server; the client never sees it.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly JwtAuthenticationStateProvider _stateProvider;
        private readonly IKeyDerivationService _kdf;
        private readonly IUserKeypairService _keypair;
        private readonly IStretchedKeyStore _keyStore;
        private readonly IVaultKeyCache _vaultKeyCache;
        private readonly IOwnKeypairCache _ownKeypairCache;

        public AuthService(HttpClient http, AuthenticationStateProvider stateProvider,
            IKeyDerivationService kdf, IUserKeypairService keypair, IStretchedKeyStore keyStore,
            IVaultKeyCache vaultKeyCache, IOwnKeypairCache ownKeypairCache)
        {
            _http = http;
            _stateProvider = (JwtAuthenticationStateProvider)stateProvider;
            _kdf = kdf;
            _keypair = keypair;
            _keyStore = keyStore;
            _vaultKeyCache = vaultKeyCache;
            _ownKeypairCache = ownKeypairCache;
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AuthResult.Fail(await ReadErrorAsync(response, "Invalid credentials."));
            }
            // Unlike Register, Login doesn't already have the salt locally — it lives on the
            // account being logged into — so the response body has to be read here to derive
            // the stretched master key, not just relied on as a side channel for the cookie.
            return await HandleSuccessAsync(response, async r =>
            {
                var auth = await r.Content.ReadFromJsonAsync<AuthResponse>();
                if (auth?.KeySalt is not null)
                {
                    _keyStore.Set(await _kdf.DeriveKeyAsync(request.Password, Convert.FromBase64String(auth.KeySalt)));
                }
                _ownKeypairCache.Set(auth?.PublicKey, auth?.EncryptedPrivateKey);
            });
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            var salt = _kdf.GenerateSalt();
            var stretchedMasterKey = await _kdf.DeriveKeyAsync(request.Password, salt);
            var keypair = await _keypair.GenerateAsync(stretchedMasterKey);

            request.KeySalt = Convert.ToBase64String(salt);
            request.PublicKey = keypair.PublicKeyPem;
            request.EncryptedPrivateKey = keypair.EncryptedPrivateKey;

            var response = await _http.PostAsJsonAsync("api/auth/register", request);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return AuthResult.Fail(await ReadErrorAsync(response, "The email is already registered."));
            }
            return await HandleSuccessAsync(response, _ =>
            {
                _keyStore.Set(stretchedMasterKey);
                _ownKeypairCache.Set(keypair.PublicKeyPem, keypair.EncryptedPrivateKey);
                return Task.CompletedTask;
            });
        }

        public async Task LogoutAsync()
        {
            await _http.PostAsync("api/auth/logout", null);
            _keyStore.Clear();
            _vaultKeyCache.Clear();
            _ownKeypairCache.Clear();
            _stateProvider.NotifyLoggedOut();
        }

        private async Task<AuthResult> HandleSuccessAsync(
            HttpResponseMessage response, Func<HttpResponseMessage, Task> onSuccess)
        {
            if (!response.IsSuccessStatusCode)
            {
                return AuthResult.Fail(await ReadErrorAsync(response, "Unexpected error. Please try again."));
            }

            await onSuccess(response);

            // Beyond that, the response body is not read: the session lives in the HttpOnly
            // cookie the server just set, and RefreshAsync re-reads the user from /api/auth/me.
            await _stateProvider.RefreshAsync();
            return AuthResult.Ok();
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
