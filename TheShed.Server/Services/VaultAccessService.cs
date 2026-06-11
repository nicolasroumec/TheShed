using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Shared.Models.Enums;

namespace TheShed.Server.Services
{
    public class VaultAccessService : IVaultAccessService
    {
        private readonly TheShedContext _db;

        public VaultAccessService(TheShedContext db) => _db = db;

        public async Task<VaultAccess> GetAccessAsync(int vaultId, int userId, CancellationToken ct = default)
        {
            // Soft-deleted vaults/members are filtered out by the global query filter.
            var vault = await _db.Vaults.FirstOrDefaultAsync(v => v.Id == vaultId, ct);
            if (vault is null)
            {
                return VaultAccess.None;
            }

            // The owner always has full access.
            if (vault.OwnerId == userId)
            {
                return VaultAccess.Write;
            }

            var membership = await _db.VaultMembers
                .FirstOrDefaultAsync(m => m.VaultId == vaultId && m.UserId == userId, ct);
            if (membership is null)
            {
                return VaultAccess.None;
            }

            return membership.Role == VaultRole.Editor ? VaultAccess.Write : VaultAccess.Read;
        }
    }
}
