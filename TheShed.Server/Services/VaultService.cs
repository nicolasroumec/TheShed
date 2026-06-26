using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;

namespace TheShed.Server.Services
{
    public class VaultService : IVaultService
    {
        private readonly TheShedContext _db;
        private readonly IVaultAccessService _access;

        public VaultService(TheShedContext db, IVaultAccessService access)
        {
            _db = db;
            _access = access;
        }

        public async Task<IReadOnlyList<VaultListItem>> ListAsync(int userId, CancellationToken ct = default)
        {
            // Soft-deleted vaults/members are excluded by the global query filter.
            var owned = await _db.Vaults
                .Where(v => v.OwnerId == userId)
                .Select(v => new VaultListItem
                {
                    Id = v.Id,
                    Name = v.Name,
                    Description = v.Description,
                    IsOwner = true,
                    CanWrite = true
                })
                .ToListAsync(ct);

            var shared = await _db.VaultMembers
                .Where(m => m.UserId == userId)
                .Select(m => new VaultListItem
                {
                    Id = m.Vault.Id,
                    Name = m.Vault.Name,
                    Description = m.Vault.Description,
                    IsOwner = false,
                    CanWrite = m.Role == VaultRole.Editor
                })
                .ToListAsync(ct);

            // ponytail: a user is never both owner and member of the same vault, so no dedup.
            return owned.Concat(shared).OrderBy(v => v.Name).ToList();
        }

        public async Task<EntryResult<VaultResponse>> GetAsync(int userId, int vaultId, CancellationToken ct = default)
        {
            var access = await _access.GetAccessAsync(vaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                // Hide the existence of vaults the user cannot see.
                return EntryResult<VaultResponse>.Fail(EntryError.NotFound);
            }

            var vault = await _db.Vaults.FirstAsync(v => v.Id == vaultId, ct);
            return EntryResult<VaultResponse>.Ok(ToResponse(vault, userId, access));
        }

        public async Task<VaultResponse> CreateAsync(int userId, VaultCreateRequest request, CancellationToken ct = default)
        {
            var vault = new Vault
            {
                OwnerId = userId,
                Name = request.Name.Trim(),
                Description = request.Description
            };

            _db.Vaults.Add(vault);
            await _db.SaveChangesAsync(ct);

            return ToResponse(vault, userId, VaultAccess.Write);
        }

        public async Task<EntryResult<VaultResponse>> UpdateAsync(int userId, int vaultId, VaultUpdateRequest request, CancellationToken ct = default)
        {
            var denied = await OwnerOnlyAsync(vaultId, userId, ct);
            if (denied is not null)
            {
                return EntryResult<VaultResponse>.Fail(denied.Value);
            }

            var vault = await _db.Vaults.FirstAsync(v => v.Id == vaultId, ct);
            vault.Name = request.Name.Trim();
            vault.Description = request.Description;
            await _db.SaveChangesAsync(ct);

            return EntryResult<VaultResponse>.Ok(ToResponse(vault, userId, VaultAccess.Write));
        }

        public async Task<EntryResult<bool>> DeleteAsync(int userId, int vaultId, CancellationToken ct = default)
        {
            var denied = await OwnerOnlyAsync(vaultId, userId, ct);
            if (denied is not null)
            {
                return EntryResult<bool>.Fail(denied.Value);
            }

            var vault = await _db.Vaults.FirstAsync(v => v.Id == vaultId, ct);
            // Soft delete: the SaveChanges override turns this into an IsDeleted update.
            _db.Vaults.Remove(vault);
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }

        /// <summary>Owner-only gate for mutating the vault itself (rename/delete). Returns null
        /// if the caller owns the vault, NotFound if they have no access (existence hidden), or
        /// Forbidden if they can see it (a member) but do not own it.</summary>
        private async Task<EntryError?> OwnerOnlyAsync(int vaultId, int userId, CancellationToken ct)
        {
            var access = await _access.GetAccessAsync(vaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryError.NotFound;
            }

            var vault = await _db.Vaults.FirstAsync(v => v.Id == vaultId, ct);
            return vault.OwnerId == userId ? null : EntryError.Forbidden;
        }

        private static VaultResponse ToResponse(Vault vault, int userId, VaultAccess access) => new()
        {
            Id = vault.Id,
            Name = vault.Name,
            Description = vault.Description,
            IsOwner = vault.OwnerId == userId,
            CanWrite = access == VaultAccess.Write,
            CreatedAt = vault.CreatedAt,
            UpdatedAt = vault.UpdatedAt
        };
    }
}
