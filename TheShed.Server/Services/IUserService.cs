using TheShed.Shared.Models.DTOs.Users;

namespace TheShed.Server.Services
{
    public interface IUserService
    {
        /// <summary>Looks up a user by email and returns their RSA public key, or null if no
        /// such user exists or they predate keypairs (Sprint 25) and have none yet.</summary>
        Task<UserPublicKeyResponse?> GetPublicKeyAsync(string email, CancellationToken ct = default);
    }
}
