using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Security;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Services
{
    public class PasswordEntryService : IPasswordEntryService
    {
        private readonly TheShedContext _db;
        private readonly IEncryptionService _encryption;
        private readonly IVaultAccessService _access;

        public PasswordEntryService(TheShedContext db, IEncryptionService encryption, IVaultAccessService access)
        {
            _db = db;
            _encryption = encryption;
            _access = access;
        }

        public async Task<EntryResult<IReadOnlyList<EntryListItem>>> ListAsync(int userId, int vaultId, CancellationToken ct = default)
        {
            var access = await _access.GetAccessAsync(vaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                // Hide the existence of vaults the user cannot see.
                return EntryResult<IReadOnlyList<EntryListItem>>.Fail(EntryError.NotFound);
            }

            var items = await _db.PasswordEntries
                .Where(e => e.VaultId == vaultId)
                .OrderBy(e => e.Name)
                .Select(e => new EntryListItem
                {
                    Id = e.Id,
                    Name = e.Name,
                    Username = e.Username,
                    Url = e.Url,
                    IsFavorite = e.IsFavorite
                })
                .ToListAsync(ct);

            return EntryResult<IReadOnlyList<EntryListItem>>.Ok(items);
        }

        public async Task<EntryResult<EntryResponse>> GetAsync(int userId, int entryId, CancellationToken ct = default)
        {
            var entry = await _db.PasswordEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct);
            if (entry is null)
            {
                return EntryResult<EntryResponse>.Fail(EntryError.NotFound);
            }

            var access = await _access.GetAccessAsync(entry.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryResult<EntryResponse>.Fail(EntryError.NotFound);
            }

            return EntryResult<EntryResponse>.Ok(ToResponse(entry));
        }

        public async Task<EntryResult<EntryResponse>> CreateAsync(int userId, EntryCreateRequest request, CancellationToken ct = default)
        {
            var access = await _access.GetAccessAsync(request.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryResult<EntryResponse>.Fail(EntryError.NotFound);
            }
            if (access != VaultAccess.Write)
            {
                return EntryResult<EntryResponse>.Fail(EntryError.Forbidden);
            }

            var entry = new PasswordEntry
            {
                VaultId = request.VaultId,
                Name = request.Name.Trim(),
                Username = request.Username,
                PasswordEncrypted = _encryption.Encrypt(request.Password),
                Url = request.Url,
                Notes = request.Notes,
                IsFavorite = request.IsFavorite
            };

            _db.PasswordEntries.Add(entry);
            await _db.SaveChangesAsync(ct);

            return EntryResult<EntryResponse>.Ok(ToResponse(entry));
        }

        public async Task<EntryResult<EntryResponse>> UpdateAsync(int userId, int entryId, EntryUpdateRequest request, CancellationToken ct = default)
        {
            var entry = await _db.PasswordEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct);
            if (entry is null)
            {
                return EntryResult<EntryResponse>.Fail(EntryError.NotFound);
            }

            var access = await _access.GetAccessAsync(entry.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryResult<EntryResponse>.Fail(EntryError.NotFound);
            }
            if (access != VaultAccess.Write)
            {
                return EntryResult<EntryResponse>.Fail(EntryError.Forbidden);
            }

            entry.Name = request.Name.Trim();
            entry.Username = request.Username;
            entry.PasswordEncrypted = _encryption.Encrypt(request.Password);
            entry.Url = request.Url;
            entry.Notes = request.Notes;
            entry.IsFavorite = request.IsFavorite;

            await _db.SaveChangesAsync(ct);

            return EntryResult<EntryResponse>.Ok(ToResponse(entry));
        }

        public async Task<EntryResult<bool>> DeleteAsync(int userId, int entryId, CancellationToken ct = default)
        {
            var entry = await _db.PasswordEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct);
            if (entry is null)
            {
                return EntryResult<bool>.Fail(EntryError.NotFound);
            }

            var access = await _access.GetAccessAsync(entry.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryResult<bool>.Fail(EntryError.NotFound);
            }
            if (access != VaultAccess.Write)
            {
                return EntryResult<bool>.Fail(EntryError.Forbidden);
            }

            // Soft delete: the SaveChanges override turns this into an IsDeleted update.
            _db.PasswordEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }

        /// <summary>Maps an entry to its detail DTO, decrypting the stored password.</summary>
        private EntryResponse ToResponse(PasswordEntry entry) => new()
        {
            Id = entry.Id,
            VaultId = entry.VaultId,
            Name = entry.Name,
            Username = entry.Username,
            Password = _encryption.Decrypt(entry.PasswordEncrypted),
            Url = entry.Url,
            Notes = entry.Notes,
            IsFavorite = entry.IsFavorite,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }
}
