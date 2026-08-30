using System.Net.Http.Json;
using System.Security.Cryptography;
using TheShed.Shared.Models.DTOs.Auth;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IReauthGate"/>
    public class ReauthGate : IReauthGate
    {
        // ponytail: one window for the whole session rather than per entry or per action. Scoping
        // it finer means a dictionary and an eviction policy for a distinction nobody has asked
        // for. Shorter than the idle lock on purpose — this guards a single reveal, not a session.
        private static readonly TimeSpan ConfirmationWindow = TimeSpan.FromMinutes(5);

        private readonly HttpClient _http;
        private readonly IModalService _modal;
        private readonly IKeyDerivationService _kdf;
        private readonly IStretchedKeyStore _keyStore;

        private DateTimeOffset _confirmedUntil = DateTimeOffset.MinValue;

        public ReauthGate(HttpClient http, IModalService modal, IKeyDerivationService kdf, IStretchedKeyStore keyStore)
        {
            _http = http;
            _modal = modal;
            _kdf = kdf;
            _keyStore = keyStore;
        }

        public async Task<bool> EnsureAsync()
        {
            if (DateTimeOffset.UtcNow < _confirmedUntil)
            {
                return true;
            }

            // Before prompting, not inside the loop below: with no key to compare against, every
            // attempt would come back wrong and the dialog would insist the right password is the
            // wrong one, forever. The unlock prompt is what this session actually needs.
            if (_keyStore.Get() is null)
            {
                return false;
            }

            // A wrong password re-prompts with the reason in the dialog the user is already
            // looking at, rather than reporting it through the caller — which keeps every call
            // site a single line and stops a typo from looking like a button that does nothing.
            var message = "Enter your master password to reveal it.";
            while (true)
            {
                var password = await _modal.PromptPasswordAsync(message);
                if (string.IsNullOrEmpty(password))
                {
                    return false;
                }

                if (await MatchesAsync(password))
                {
                    _confirmedUntil = DateTimeOffset.UtcNow.Add(ConfirmationWindow);
                    return true;
                }

                message = "That is not your master password. Try again.";
            }
        }

        private async Task<bool> MatchesAsync(string password)
        {
            // Unlike the unlock prompt, this session already holds the right key, so the check is
            // a comparison instead of a trial decryption: the same password and salt derive the
            // same key. Fixed-time so a wrong guess cannot be narrowed down by how long it took.
            var current = _keyStore.Get();
            if (current is null)
            {
                return false; // locked between the prompt opening and the answer coming back
            }

            var me = await _http.GetFromJsonAsync<AuthResponse>("api/auth/me");
            if (me?.KeySalt is null)
            {
                return false;
            }

            var derived = await _kdf.DeriveKeyAsync(password, Convert.FromBase64String(me.KeySalt));
            return CryptographicOperations.FixedTimeEquals(derived, current);
        }
    }
}
