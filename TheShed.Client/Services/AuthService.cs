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
        private readonly IAesGcmService _aesGcm;
        private readonly CsrfHandler _csrf;

        public AuthService(HttpClient http, AuthenticationStateProvider stateProvider,
            IKeyDerivationService kdf, IUserKeypairService keypair, IStretchedKeyStore keyStore,
            IVaultKeyCache vaultKeyCache, IOwnKeypairCache ownKeypairCache, IAesGcmService aesGcm,
            CsrfHandler csrf)
        {
            _http = http;
            _stateProvider = (JwtAuthenticationStateProvider)stateProvider;
            _kdf = kdf;
            _keypair = keypair;
            _keyStore = keyStore;
            _vaultKeyCache = vaultKeyCache;
            _ownKeypairCache = ownKeypairCache;
            _aesGcm = aesGcm;
            _csrf = csrf;
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            // The salt comes first (prelogin) because the master password itself never goes to
            // the server — only the auth hash derived from it, which needs the salt. Unknown
            // emails get a fake salt, so this call alone reveals nothing; the login below just
            // fails with the usual 401.
            var prelogin = await _http.GetAsync($"api/auth/prelogin?email={Uri.EscapeDataString(request.Email)}");
            if (!prelogin.IsSuccessStatusCode)
            {
                return AuthResult.Fail(await ReadErrorAsync(prelogin, "Unexpected error. Please try again."));
            }
            var keySalt = (await prelogin.Content.ReadFromJsonAsync<PreloginResponse>())!.KeySalt;
            var stretchedMasterKey = await _kdf.DeriveKeyAsync(request.Password, Convert.FromBase64String(keySalt));

            // A copy, not the form's model: writing the hash into request.Password would show it
            // in the password field after a failed attempt.
            var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest
            {
                Email = request.Email,
                Password = AuthHash.Compute(stretchedMasterKey)
            });
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AuthResult.Fail(await ReadErrorAsync(response, "Invalid credentials."));
            }
            return await HandleSuccessAsync(response, async r =>
            {
                var auth = await r.Content.ReadFromJsonAsync<AuthResponse>();
                _keyStore.Set(stretchedMasterKey);
                _ownKeypairCache.Set(auth?.PublicKey, auth?.EncryptedPrivateKey);
            });
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            var salt = _kdf.GenerateSalt();
            var stretchedMasterKey = await _kdf.DeriveKeyAsync(request.Password, salt);
            var keypair = await _keypair.GenerateAsync(stretchedMasterKey);

            // Same copy-not-mutate reasoning as LoginAsync.
            var response = await _http.PostAsJsonAsync("api/auth/register", new RegisterRequest
            {
                Username = request.Username,
                Email = request.Email,
                Password = AuthHash.Compute(stretchedMasterKey),
                KeySalt = Convert.ToBase64String(salt),
                PublicKey = keypair.PublicKeyPem,
                EncryptedPrivateKey = keypair.EncryptedPrivateKey
            });
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

        public async Task<AuthResult> UnlockAsync(string masterPassword)
        {
            AuthResponse? me;
            try
            {
                me = await _http.GetFromJsonAsync<AuthResponse>("api/auth/me");
            }
            catch (HttpRequestException)
            {
                // The cookie expired while the tab sat open: there is no session left to unlock.
                return AuthResult.Fail("Your session expired. Sign in again.");
            }

            if (me?.KeySalt is null || me.EncryptedPrivateKey is null)
            {
                return AuthResult.Fail("This account has no encryption keys on the server. Sign in again.");
            }

            var stretchedMasterKey = await _kdf.DeriveKeyAsync(masterPassword, Convert.FromBase64String(me.KeySalt));

            // The server could confirm the password — it Argon2-verifies it on login — but asking it
            // would put the master password back on the wire and burn the login rate limit on every
            // unlock, and unlocking happens far more often than signing in. The check comes free from
            // the crypto instead: a key derived from the wrong password fails the AES-GCM auth tag on
            // the account's own wrapped private key. Nothing extra is stored to make this work.
            try
            {
                await _aesGcm.DecryptAsync(stretchedMasterKey, me.EncryptedPrivateKey);
            }
            catch (Exception)
            {
                return AuthResult.Fail("Incorrect master password.");
            }

            _keyStore.Set(stretchedMasterKey);
            _ownKeypairCache.Set(me.PublicKey, me.EncryptedPrivateKey);
            return AuthResult.Ok();
        }

        public async Task<AuthResult> ChangePasswordAsync(ChangePasswordRequest request)
        {
            AuthResponse? me;
            try
            {
                me = await _http.GetFromJsonAsync<AuthResponse>("api/auth/me");
            }
            catch (HttpRequestException)
            {
                return AuthResult.Fail("Your session expired. Sign in again.");
            }

            if (me?.KeySalt is null || me.EncryptedPrivateKey is null)
            {
                return AuthResult.Fail("This account has no encryption keys on the server. Sign in again.");
            }

            var currentKey = await _kdf.DeriveKeyAsync(request.CurrentPassword, Convert.FromBase64String(me.KeySalt));
            var newSalt = _kdf.GenerateSalt();
            var newKey = await _kdf.DeriveKeyAsync(request.NewPassword, newSalt);

            // A wrong current password fails the AES-GCM tag right here, same check as
            // UnlockAsync — no request sent, no rate-limit permit burned.
            string newEncryptedPrivateKey;
            try
            {
                newEncryptedPrivateKey = await _keypair.RewrapAsync(currentKey, newKey, me.EncryptedPrivateKey);
            }
            catch (Exception)
            {
                return AuthResult.Fail("Current password is incorrect.");
            }

            // Owned vaults wrap their key with the stretched key too (IVaultKeyService), so every
            // one of them — trashed ones included — has to move to the new key in the same request.
            var vaultKeys = new List<OwnedVaultKey>();
            try
            {
                var owned = await _http.GetFromJsonAsync<List<OwnedVaultKey>>("api/auth/owned-vault-keys") ?? [];
                foreach (var key in owned)
                {
                    vaultKeys.Add(new OwnedVaultKey
                    {
                        VaultId = key.VaultId,
                        WrappedKey = await _keypair.RewrapAsync(currentKey, newKey, key.WrappedKey)
                    });
                }
            }
            catch (Exception)
            {
                return AuthResult.Fail("Couldn't re-encrypt your vault keys. Nothing was changed.");
            }

            // Same copy-not-mutate reasoning as LoginAsync.
            var response = await _http.PostAsJsonAsync("api/auth/change-password", new ChangePasswordRequest
            {
                CurrentPassword = AuthHash.Compute(currentKey),
                NewPassword = AuthHash.Compute(newKey),
                NewKeySalt = Convert.ToBase64String(newSalt),
                NewEncryptedPrivateKey = newEncryptedPrivateKey,
                VaultKeys = vaultKeys
            });
            return await HandleSuccessAsync(response, _ =>
            {
                _keyStore.Set(newKey);
                _ownKeypairCache.Set(me.PublicKey, newEncryptedPrivateKey);
                return Task.CompletedTask;
            });
        }

        public void Lock()
        {
            _keyStore.Clear();
            _vaultKeyCache.Clear();
            _ownKeypairCache.Clear();
        }

        public async Task LogoutAsync()
        {
            await _http.PostAsync("api/auth/logout", null);
            // The cached CSRF token was bound to this (now former) authenticated identity — see
            // CsrfHandler.Invalidate.
            _csrf.Invalidate();
            Lock();
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
            // The CSRF token cached before this call was anonymous (or a different user); the
            // cookie the server just set flips the identity, so the next mutation needs a fresh
            // one — see CsrfHandler.Invalidate.
            _csrf.Invalidate();

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
