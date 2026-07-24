using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Security;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Models.DTOs.Tags;
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

        public async Task<EntryResult<IReadOnlyList<EntryListItem>>> ListAsync(int userId, int vaultId, int? tagId = null, string? search = null, CancellationToken ct = default)
        {
            var access = await _access.GetAccessAsync(vaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                // Hide the existence of vaults the user cannot see.
                return EntryResult<IReadOnlyList<EntryListItem>>.Fail(EntryError.NotFound);
            }

            var query = _db.PasswordEntries.Where(e => e.VaultId == vaultId);
            if (tagId is not null)
            {
                query = query.Where(e => e.Tags.Any(pet => pet.TagId == tagId));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.Name.Contains(search) ||
                    e.Username.Contains(search) ||
                    (e.Url != null && e.Url.Contains(search)));
            }

            var items = await query
                .OrderByDescending(e => e.IsFavorite)
                .ThenBy(e => e.Name)
                .Select(e => new EntryListItem
                {
                    Id = e.Id,
                    Name = e.Name,
                    Username = e.Username,
                    Url = e.Url,
                    IsFavorite = e.IsFavorite,
                    Tags = e.Tags
                        .Where(pet => !pet.Tag.IsDeleted)
                        .Select(pet => new TagResponse { Id = pet.Tag.Id, Name = pet.Tag.Name })
                        .OrderBy(t => t.Name)
                        .ToList(),
                    PasswordChangedAt = e.History
                        .OrderByDescending(h => h.CreatedAt)
                        .Select(h => (DateTime?)h.CreatedAt)
                        .FirstOrDefault() ?? e.CreatedAt
                })
                .ToListAsync(ct);

            return EntryResult<IReadOnlyList<EntryListItem>>.Ok(items);
        }

        public async Task<EntryResult<EntryResponse>> GetAsync(int userId, int entryId, CancellationToken ct = default)
        {
            var (entry, error) = await LoadForAccessAsync(userId, entryId, requireWrite: false, ct);
            if (entry is null)
            {
                return EntryResult<EntryResponse>.Fail(error);
            }

            var passwordChangedAt = await GetPasswordChangedAtAsync(entry, ct);
            return EntryResult<EntryResponse>.Ok(ToResponse(entry, await LoadTagsAsync(entry.Id, ct), passwordChangedAt));
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

            // A freshly created entry has no tags or history yet.
            return EntryResult<EntryResponse>.Ok(ToResponse(entry, [], entry.CreatedAt));
        }

        public async Task<EntryResult<EntryResponse>> UpdateAsync(int userId, int entryId, EntryUpdateRequest request, CancellationToken ct = default)
        {
            var (entry, error) = await LoadForAccessAsync(userId, entryId, requireWrite: true, ct);
            if (entry is null)
            {
                return EntryResult<EntryResponse>.Fail(error);
            }

            if (_encryption.Decrypt(entry.PasswordEncrypted) != request.Password)
            {
                // Snapshot the outgoing password before it's overwritten.
                // ponytail: no cap on versions kept per entry; purge/limit if the table ever grows enough to matter.
                _db.EntryHistory.Add(new EntryHistory
                {
                    PasswordEntryId = entry.Id,
                    PasswordEncrypted = entry.PasswordEncrypted,
                    ChangedByUserId = userId
                });
            }

            entry.Name = request.Name.Trim();
            entry.Username = request.Username;
            entry.PasswordEncrypted = _encryption.Encrypt(request.Password);
            entry.Url = request.Url;
            entry.Notes = request.Notes;
            entry.IsFavorite = request.IsFavorite;

            await _db.SaveChangesAsync(ct);

            var passwordChangedAt = await GetPasswordChangedAtAsync(entry, ct);
            return EntryResult<EntryResponse>.Ok(ToResponse(entry, await LoadTagsAsync(entry.Id, ct), passwordChangedAt));
        }

        public async Task<EntryResult<bool>> DeleteAsync(int userId, int entryId, CancellationToken ct = default)
        {
            var (entry, error) = await LoadForAccessAsync(userId, entryId, requireWrite: true, ct);
            if (entry is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            // Soft delete: the SaveChanges override turns this into an IsDeleted update.
            _db.PasswordEntries.Remove(entry);
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }

        public async Task<EntryResult<bool>> AddTagAsync(int userId, int entryId, int tagId, CancellationToken ct = default)
        {
            var (entry, error) = await LoadForAccessAsync(userId, entryId, requireWrite: true, ct);
            if (entry is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            // The tag must be one of the caller's own tags.
            var tagOwned = await _db.Tags.AnyAsync(t => t.Id == tagId && t.UserId == userId, ct);
            if (!tagOwned)
            {
                return EntryResult<bool>.Fail(EntryError.NotFound);
            }

            var alreadyTagged = await _db.PasswordEntryTags
                .AnyAsync(pet => pet.PasswordEntryId == entryId && pet.TagId == tagId, ct);
            if (!alreadyTagged) // idempotent: assigning an already-present tag is a no-op
            {
                _db.PasswordEntryTags.Add(new PasswordEntryTag { PasswordEntryId = entryId, TagId = tagId });
                await _db.SaveChangesAsync(ct);
            }

            return EntryResult<bool>.Ok(true);
        }

        public async Task<EntryResult<bool>> RemoveTagAsync(int userId, int entryId, int tagId, CancellationToken ct = default)
        {
            var (entry, error) = await LoadForAccessAsync(userId, entryId, requireWrite: true, ct);
            if (entry is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            var link = await _db.PasswordEntryTags
                .FirstOrDefaultAsync(pet => pet.PasswordEntryId == entryId && pet.TagId == tagId, ct);
            if (link is not null) // idempotent: removing an absent tag is a no-op
            {
                // Join rows are not soft-deleted; drop the link outright.
                _db.PasswordEntryTags.Remove(link);
                await _db.SaveChangesAsync(ct);
            }

            return EntryResult<bool>.Ok(true);
        }

        public async Task<EntryResult<bool>> SetFavoriteAsync(int userId, int entryId, bool isFavorite, CancellationToken ct = default)
        {
            var (entry, error) = await LoadForAccessAsync(userId, entryId, requireWrite: true, ct);
            if (entry is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            if (entry.IsFavorite != isFavorite) // idempotent: setting the same value is a no-op
            {
                entry.IsFavorite = isFavorite;
                await _db.SaveChangesAsync(ct);
            }

            return EntryResult<bool>.Ok(true);
        }

        public async Task<EntryResult<IReadOnlyList<EntryHistoryItem>>> GetHistoryAsync(int userId, int entryId, CancellationToken ct = default)
        {
            var (entry, error) = await LoadForAccessAsync(userId, entryId, requireWrite: false, ct);
            if (entry is null)
            {
                return EntryResult<IReadOnlyList<EntryHistoryItem>>.Fail(error);
            }

            var items = await _db.EntryHistory
                .Where(h => h.PasswordEntryId == entryId)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new EntryHistoryItem
                {
                    Id = h.Id,
                    CreatedAt = h.CreatedAt,
                    ChangedByUsername = h.ChangedBy.Username
                })
                .ToListAsync(ct);

            return EntryResult<IReadOnlyList<EntryHistoryItem>>.Ok(items);
        }

        public async Task<EntryResult<EntryHistoryDetail>> GetHistoryEntryAsync(int userId, int entryId, int historyId, CancellationToken ct = default)
        {
            var (entry, error) = await LoadForAccessAsync(userId, entryId, requireWrite: false, ct);
            if (entry is null)
            {
                return EntryResult<EntryHistoryDetail>.Fail(error);
            }

            var history = await _db.EntryHistory
                .Where(h => h.Id == historyId && h.PasswordEntryId == entryId)
                .Select(h => new EntryHistoryDetail
                {
                    Id = h.Id,
                    Password = h.PasswordEncrypted,
                    CreatedAt = h.CreatedAt,
                    ChangedByUsername = h.ChangedBy.Username
                })
                .FirstOrDefaultAsync(ct);
            if (history is null)
            {
                return EntryResult<EntryHistoryDetail>.Fail(EntryError.NotFound);
            }

            history.Password = _encryption.Decrypt(history.Password);
            return EntryResult<EntryHistoryDetail>.Ok(history);
        }

        /// <summary>When the current password became active: the latest EntryHistory
        /// snapshot's CreatedAt, or the entry's own CreatedAt if it never changed.</summary>
        private async Task<DateTime> GetPasswordChangedAtAsync(PasswordEntry entry, CancellationToken ct)
        {
            var lastChange = await _db.EntryHistory
                .Where(h => h.PasswordEntryId == entry.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => (DateTime?)h.CreatedAt)
                .FirstOrDefaultAsync(ct);
            return lastChange ?? entry.CreatedAt;
        }

        /// <summary>Loads an entry the caller can access, or the reason it's unavailable
        /// (not found/no access → NotFound, read-only access when write is required → Forbidden).</summary>
        private async Task<(PasswordEntry? Entry, EntryError Error)> LoadForAccessAsync(int userId, int entryId, bool requireWrite, CancellationToken ct)
        {
            var entry = await _db.PasswordEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct);
            if (entry is null)
            {
                return (null, EntryError.NotFound);
            }

            var access = await _access.GetAccessAsync(entry.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return (null, EntryError.NotFound);
            }
            if (requireWrite && access != VaultAccess.Write)
            {
                return (null, EntryError.Forbidden);
            }

            return (entry, EntryError.None);
        }

        /// <summary>Loads an entry's tags, skipping any that were soft-deleted.</summary>
        private Task<List<TagResponse>> LoadTagsAsync(int entryId, CancellationToken ct) =>
            _db.PasswordEntryTags
                .Where(pet => pet.PasswordEntryId == entryId)
                .Join(_db.Tags, pet => pet.TagId, t => t.Id, (pet, t) => new TagResponse { Id = t.Id, Name = t.Name })
                .OrderBy(t => t.Name)
                .ToListAsync(ct);

        /// <summary>Maps an entry to its detail DTO, decrypting the stored password.</summary>
        private EntryResponse ToResponse(PasswordEntry entry, List<TagResponse> tags, DateTime passwordChangedAt) => new()
        {
            Id = entry.Id,
            VaultId = entry.VaultId,
            Name = entry.Name,
            Username = entry.Username,
            Password = _encryption.Decrypt(entry.PasswordEncrypted),
            Url = entry.Url,
            Notes = entry.Notes,
            IsFavorite = entry.IsFavorite,
            Tags = tags,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            PasswordChangedAt = passwordChangedAt
        };
    }
}
