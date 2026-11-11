using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Security;
using TheShed.Shared.Models.DTOs.Auth;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly TheShedContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtTokenService _jwt;
        private readonly byte[] _fakeSaltKey;
        private readonly TimeProvider _time;

        // Same length as IKeyDerivationService.GenerateSalt, so fake and real salts look alike.
        private const int SaltSize = 16;

        public AuthService(TheShedContext db, IPasswordHasher hasher, IJwtTokenService jwt,
            IOptions<JwtSettings> jwtSettings, TimeProvider time)
        {
            _time = time;
            _db = db;
            _hasher = hasher;
            _jwt = jwt;
            // ponytail: reuses the JWT signing key as the fake-salt HMAC key (domain-separated by
            // the "prelogin-salt:" prefix below) instead of a dedicated secret. HMAC output doesn't
            // reveal its key; add a separate setting if the two ever need to rotate independently.
            _fakeSaltKey = Encoding.UTF8.GetBytes(jwtSettings.Value.Key);
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            {
                return new AuthResult(false, AuthError.EmailInUse, null);
            }

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = email,
                PasswordHash = _hasher.Hash(request.Password),
                KeySalt = request.KeySalt,
                PublicKey = request.PublicKey,
                EncryptedPrivateKey = request.EncryptedPrivateKey
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            return Success(user);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user is null || !user.IsActive || !_hasher.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResult(false, AuthError.InvalidCredentials, null);
            }

            // Only reached with a correct password, so "this account has 2FA" is never revealed
            // to someone who doesn't already know the password.
            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrEmpty(request.TotpCode))
                {
                    return new AuthResult(false, AuthError.TwoFactorRequired, null);
                }
                if (!TryConsumeCode(user, request.TotpCode))
                {
                    return new AuthResult(false, AuthError.InvalidTwoFactorCode, null);
                }
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Success(user);
        }

        public async Task<string> GetPreloginSaltAsync(string email, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var salt = await _db.Users
                .Where(u => u.Email == normalized)
                .Select(u => u.KeySalt)
                .FirstOrDefaultAsync(ct);

            return salt ?? Convert.ToBase64String(
                HMACSHA256.HashData(_fakeSaltKey, Encoding.UTF8.GetBytes("prelogin-salt:" + normalized))[..SaltSize]);
        }

        public async Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null || !user.IsActive || !_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return new AuthResult(false, AuthError.InvalidCredentials, null);
            }

            // Owned vault keys are wrapped with the stretched key, so they must all move with it.
            // Exact set required: a vault created (or restored) between the client's fetch and
            // this call would otherwise keep a wrap nobody can open anymore.
            var wraps = await OwnedWraps(userId).ToListAsync(ct);
            var newWraps = new Dictionary<int, string>();
            if (request.VaultKeys.Any(k => string.IsNullOrEmpty(k.WrappedKey) || !newWraps.TryAdd(k.VaultId, k.WrappedKey)) ||
                wraps.Count != newWraps.Count || wraps.Any(w => !newWraps.ContainsKey(w.VaultId)))
            {
                return new AuthResult(false, AuthError.VaultKeysOutOfDate, null);
            }

            // Bumping TokenVersion signs out every other session (N3) — they'd otherwise carry a
            // stale keySalt claim. This one survives: Success(user) issues a token with the new
            // version.
            user.TokenVersion++;
            user.PasswordHash = _hasher.Hash(request.NewPassword);
            user.KeySalt = request.NewKeySalt;
            user.EncryptedPrivateKey = request.NewEncryptedPrivateKey;
            foreach (var wrap in wraps)
            {
                wrap.WrappedKey = newWraps[wrap.VaultId];
            }
            await _db.SaveChangesAsync(ct);

            return Success(user);
        }

        public async Task SignOutEverywhereAsync(int userId, CancellationToken ct = default) =>
            await _db.Users.Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1), ct);

        public async Task<IReadOnlyList<OwnedVaultKey>> GetOwnedVaultKeysAsync(int userId, CancellationToken ct = default) =>
            await OwnedWraps(userId)
                .Select(w => new OwnedVaultKey { VaultId = w.VaultId, WrappedKey = w.WrappedKey })
                .ToListAsync(ct);

        // IgnoreQueryFilters: trashed vaults can be restored (TrashService), so their wraps count.
        private IQueryable<VaultKeyWrap> OwnedWraps(int userId) =>
            _db.VaultKeyWraps.IgnoreQueryFilters()
                .Where(w => !w.IsDeleted && w.UserId == userId && w.Vault.OwnerId == userId);

        public Task<bool> IsTwoFactorEnabledAsync(int userId, CancellationToken ct = default) =>
            _db.Users.Where(u => u.Id == userId).Select(u => u.TwoFactorEnabled).FirstOrDefaultAsync(ct);

        public async Task<TwoFactorSetupResponse?> BeginTwoFactorSetupAsync(int userId, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
            if (user.TwoFactorEnabled)
            {
                return null;
            }

            var secret = Totp.GenerateSecret();
            user.TwoFactorSecret = Convert.ToBase64String(secret);
            user.TwoFactorLastStep = null;
            await _db.SaveChangesAsync(ct);

            var base32 = Totp.ToBase32(secret);
            var issuer = Uri.EscapeDataString("The Shed");
            var uri = $"otpauth://totp/{issuer}:{Uri.EscapeDataString(user.Email)}?secret={base32}&issuer={issuer}";
            return new TwoFactorSetupResponse(base32, uri);
        }

        public async Task<bool> EnableTwoFactorAsync(int userId, string code, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
            if (user.TwoFactorEnabled || !TryConsumeCode(user, code))
            {
                return false;
            }

            user.TwoFactorEnabled = true;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DisableTwoFactorAsync(int userId, string code, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
            if (!user.TwoFactorEnabled || !TryConsumeCode(user, code))
            {
                return false;
            }

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.TwoFactorLastStep = null;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        // Accepts the code only if it's for a step after the last one used, then records that
        // step — the caller saves. ponytail: two concurrent requests with the same code can both
        // pass (no concurrency token); the rate limit makes that window negligible.
        private bool TryConsumeCode(User user, string? code)
        {
            if (user.TwoFactorSecret is null)
            {
                return false;
            }

            var step = Totp.MatchStep(Convert.FromBase64String(user.TwoFactorSecret), code, _time.GetUtcNow());
            if (step is null || step <= user.TwoFactorLastStep)
            {
                return false;
            }

            user.TwoFactorLastStep = step;
            return true;
        }

        public async Task<(string? PublicKey, string? EncryptedPrivateKey)> GetKeypairAsync(int userId, CancellationToken ct = default)
        {
            var user = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.PublicKey, u.EncryptedPrivateKey })
                .FirstOrDefaultAsync(ct);
            return user is null ? (null, null) : (user.PublicKey, user.EncryptedPrivateKey);
        }

        private AuthResult Success(User user)
        {
            var (token, expiresAt) = _jwt.GenerateToken(user);
            return new AuthResult(true, AuthError.None, new AuthResponse
            {
                ExpiresAt = expiresAt,
                Username = user.Username,
                Email = user.Email,
                KeySalt = user.KeySalt,
                PublicKey = user.PublicKey,
                EncryptedPrivateKey = user.EncryptedPrivateKey
            }, token);
        }
    }
}
