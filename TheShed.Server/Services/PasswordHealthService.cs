using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Shared.Security;
using TheShed.Shared.Helpers;
using TheShed.Shared.Models.DTOs.Health;

namespace TheShed.Server.Services
{
    public class PasswordHealthService : IPasswordHealthService
    {
        private readonly TheShedContext _db;
        private readonly IEncryptionService _encryption;

        public PasswordHealthService(TheShedContext db, IEncryptionService encryption)
        {
            _db = db;
            _encryption = encryption;
        }

        public async Task<IReadOnlyList<PasswordHealthItem>> GetReportAsync(int userId, CancellationToken ct = default)
        {
            var vaultIds = await AccessibleVaultIdsAsync(userId, ct);

            var entries = await _db.PasswordEntries
                .Where(e => vaultIds.Contains(e.VaultId))
                .Select(e => new { e.Id, e.VaultId, e.Name, e.PasswordEncrypted })
                .ToListAsync(ct);

            // Decrypted only in memory to rate/compare; never persisted or logged.
            var decrypted = entries
                .Select(e => (e.Id, e.VaultId, e.Name, Password: _encryption.Decrypt(e.PasswordEncrypted)))
                .ToList();

            var reused = decrypted
                .GroupBy(e => e.Password)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            return decrypted
                .Select(e => new PasswordHealthItem
                {
                    EntryId = e.Id,
                    VaultId = e.VaultId,
                    EntryName = e.Name,
                    Strength = PasswordHealthChecker.EvaluateStrength(e.Password),
                    IsReused = reused.Contains(e.Password)
                })
                .OrderBy(i => i.EntryName)
                .ToList();
        }

        private async Task<HashSet<int>> AccessibleVaultIdsAsync(int userId, CancellationToken ct)
        {
            var owned = await _db.Vaults.Where(v => v.OwnerId == userId).Select(v => v.Id).ToListAsync(ct);
            var shared = await _db.VaultMembers.Where(m => m.UserId == userId).Select(m => m.VaultId).ToListAsync(ct);
            return owned.Concat(shared).ToHashSet();
        }
    }
}
