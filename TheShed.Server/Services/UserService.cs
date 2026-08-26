using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Shared.Models.DTOs.Users;

namespace TheShed.Server.Services
{
    public class UserService : IUserService
    {
        private readonly TheShedContext _db;

        public UserService(TheShedContext db) => _db = db;

        public async Task<UserPublicKeyResponse?> GetPublicKeyAsync(string email, CancellationToken ct = default)
        {
            var trimmed = email.Trim();
            return await _db.Users
                .Where(u => u.Email == trimmed && u.PublicKey != null)
                .Select(u => new UserPublicKeyResponse { UserId = u.Id, PublicKey = u.PublicKey! })
                .FirstOrDefaultAsync(ct);
        }
    }
}
