using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Shared.Models.Base;
using TheShed.Shared.Models.DTOs.Trash;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;

namespace TheShed.Server.Services
{
    public class TrashService : ITrashService
    {
        private readonly TheShedContext _db;
        private readonly IVaultAccessService _access;
        private readonly int _retentionDays;

        public TrashService(TheShedContext db, IVaultAccessService access, IOptions<TrashSettings> settings)
        {
            _db = db;
            _access = access;
            _retentionDays = settings.Value.RetentionDays;
        }

        public async Task<IReadOnlyList<TrashItem>> ListAsync(int userId, CancellationToken ct = default)
        {
            var vaults = await _db.Vaults.IgnoreQueryFilters()
                .Where(v => v.IsDeleted && v.OwnerId == userId)
                .Select(v => new TrashItem
                {
                    Id = v.Id,
                    Type = TrashItemType.Vault,
                    Name = v.Name,
                    DeletedAt = v.DeletedAt!.Value
                })
                .ToListAsync(ct);

            // A deleted vault carries its entries/notes with it (hidden by the vault's own
            // absence, not individually flagged), so only entries/notes whose vault is still
            // alive show up here on their own.
            // ponytail: duplicates IVaultAccessService's Owner/Editor -> Write rule inline,
            // since a LINQ-to-SQL query can't call into the service. Keep both in sync.
            var entries = await _db.PasswordEntries.IgnoreQueryFilters()
                .Where(e => e.IsDeleted && !e.Vault.IsDeleted &&
                    (e.Vault.OwnerId == userId || e.Vault.Members.Any(m => m.UserId == userId && m.Role == VaultRole.Editor)))
                .Select(e => new TrashItem
                {
                    Id = e.Id,
                    Type = TrashItemType.Entry,
                    Name = e.Name,
                    VaultId = e.VaultId,
                    VaultName = e.Vault.Name,
                    DeletedAt = e.DeletedAt!.Value
                })
                .ToListAsync(ct);

            var notes = await _db.SecureNotes.IgnoreQueryFilters()
                .Where(n => n.IsDeleted && !n.Vault.IsDeleted &&
                    (n.Vault.OwnerId == userId || n.Vault.Members.Any(m => m.UserId == userId && m.Role == VaultRole.Editor)))
                .Select(n => new TrashItem
                {
                    Id = n.Id,
                    Type = TrashItemType.Note,
                    Name = n.Title,
                    VaultId = n.VaultId,
                    VaultName = n.Vault.Name,
                    DeletedAt = n.DeletedAt!.Value
                })
                .ToListAsync(ct);

            var items = vaults.Concat(entries).Concat(notes).ToList();
            foreach (var item in items)
            {
                item.ExpiresAt = item.DeletedAt.AddDays(_retentionDays);
            }

            return items.OrderByDescending(i => i.DeletedAt).ToList();
        }

        public Task<EntryResult<bool>> RestoreAsync(int userId, TrashItemType type, int id, CancellationToken ct = default) =>
            type switch
            {
                TrashItemType.Vault => RestoreVaultAsync(userId, id, ct),
                TrashItemType.Entry => RestoreChildAsync(_db.PasswordEntries, userId, id, ct),
                TrashItemType.Note => RestoreChildAsync(_db.SecureNotes, userId, id, ct),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

        public Task<EntryResult<bool>> PurgeAsync(int userId, TrashItemType type, int id, CancellationToken ct = default) =>
            type switch
            {
                TrashItemType.Vault => PurgeVaultAsync(userId, id, ct),
                TrashItemType.Entry => PurgeChildAsync(_db.PasswordEntries, userId, id, ct),
                TrashItemType.Note => PurgeChildAsync(_db.SecureNotes, userId, id, ct),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

        private async Task<EntryResult<bool>> RestoreVaultAsync(int userId, int vaultId, CancellationToken ct)
        {
            var (vault, error) = await LoadDeletedVaultAsync(userId, vaultId, ct);
            if (vault is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            vault.IsDeleted = false;
            vault.DeletedAt = null;
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }

        private async Task<EntryResult<bool>> PurgeVaultAsync(int userId, int vaultId, CancellationToken ct)
        {
            var (vault, error) = await LoadDeletedVaultAsync(userId, vaultId, ct);
            if (vault is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            // Already IsDeleted: the SaveChanges override lets this go through as a real hard
            // delete, cascading to the vault's entries/notes/members at the database level.
            _db.Vaults.Remove(vault);
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }

        private async Task<(Vault? Vault, EntryError Error)> LoadDeletedVaultAsync(int userId, int vaultId, CancellationToken ct)
        {
            var vault = await _db.Vaults.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Id == vaultId && v.IsDeleted, ct);
            if (vault is null)
            {
                return (null, EntryError.NotFound);
            }
            // Vault management (D4) is owner-only; hide existence from non-owners.
            if (vault.OwnerId != userId)
            {
                return (null, EntryError.NotFound);
            }

            return (vault, EntryError.None);
        }

        private async Task<EntryResult<bool>> RestoreChildAsync<T>(DbSet<T> set, int userId, int id, CancellationToken ct)
            where T : AuditableEntity, IVaultScoped
        {
            var (item, error) = await LoadDeletedChildAsync(set, userId, id, ct);
            if (item is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            item.IsDeleted = false;
            item.DeletedAt = null;
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }

        private async Task<EntryResult<bool>> PurgeChildAsync<T>(DbSet<T> set, int userId, int id, CancellationToken ct)
            where T : AuditableEntity, IVaultScoped
        {
            var (item, error) = await LoadDeletedChildAsync(set, userId, id, ct);
            if (item is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            // Already IsDeleted: the SaveChanges override lets this go through as a real hard delete.
            set.Remove(item);
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }

        private async Task<(T? Item, EntryError Error)> LoadDeletedChildAsync<T>(DbSet<T> set, int userId, int id, CancellationToken ct)
            where T : AuditableEntity, IVaultScoped
        {
            var item = await set.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted, ct);
            if (item is null)
            {
                return (null, EntryError.NotFound);
            }

            // The vault itself must be alive: an entry/note whose vault is also deleted
            // rides along with the vault (restore/purge the vault instead).
            var access = await _access.GetAccessAsync(item.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return (null, EntryError.NotFound);
            }
            if (access != VaultAccess.Write)
            {
                return (null, EntryError.Forbidden);
            }

            return (item, EntryError.None);
        }
    }
}
