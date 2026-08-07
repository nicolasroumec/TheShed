using Microsoft.EntityFrameworkCore;
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

        public AuthService(TheShedContext db, IPasswordHasher hasher, IJwtTokenService jwt)
        {
            _db = db;
            _hasher = hasher;
            _jwt = jwt;
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
                PasswordHash = _hasher.Hash(request.Password)
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

        private AuthResult Success(User user)
        {
            var (token, expiresAt) = _jwt.GenerateToken(user);
            return new AuthResult(true, AuthError.None, new AuthResponse
            {
                ExpiresAt = expiresAt,
                Username = user.Username,
                Email = user.Email
            }, token);
        }
    }
}
