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

        // Same length as IKeyDerivationService.GenerateSalt, so fake and real salts look alike.
        private const int SaltSize = 16;

        public AuthService(TheShedContext db, IPasswordHasher hasher, IJwtTokenService jwt,
            IOptions<JwtSettings> jwtSettings)
        {
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

            // ponytail: other sessions keep working until their JWT expires, with a stale keySalt
            // claim (unlock fails there until re-login). Sign-out-everywhere is N3 (TokenVersion).
            user.PasswordHash = _hasher.Hash(request.NewPassword);
            user.KeySalt = request.NewKeySalt;
            user.EncryptedPrivateKey = request.NewEncryptedPrivateKey;
            await _db.SaveChangesAsync(ct);

            return Success(user);
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
