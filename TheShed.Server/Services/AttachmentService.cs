using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Security;
using TheShed.Shared.Models.DTOs.Attachments;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;

namespace TheShed.Server.Services
{
    public class AttachmentService : IAttachmentService
    {
        // Matches the use cases in PRODUCT.md: license PDFs, card/ID photos, short text notes.
        private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".txt"];

        private readonly TheShedContext _db;
        private readonly IEncryptionService _encryption;
        private readonly IVaultAccessService _access;
        private readonly IAttachmentStorage _storage;
        private readonly long _maxFileSizeBytes;

        public AttachmentService(TheShedContext db, IEncryptionService encryption, IVaultAccessService access,
            IAttachmentStorage storage, IOptions<AttachmentSettings> settings)
        {
            _db = db;
            _encryption = encryption;
            _access = access;
            _storage = storage;
            _maxFileSizeBytes = settings.Value.MaxFileSizeBytes;
        }

        public async Task<EntryResult<IReadOnlyList<AttachmentResponse>>> ListAsync(int userId, int entryId, CancellationToken ct = default)
        {
            var (entry, error) = await LoadEntryForAccessAsync(userId, entryId, requireWrite: false, ct);
            if (entry is null)
            {
                return EntryResult<IReadOnlyList<AttachmentResponse>>.Fail(error);
            }

            var items = await _db.Attachments
                .Where(a => a.PasswordEntryId == entryId)
                .OrderBy(a => a.FileName)
                .Select(a => new AttachmentResponse
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileSizeBytes = a.FileSizeBytes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(ct);

            return EntryResult<IReadOnlyList<AttachmentResponse>>.Ok(items);
        }

        public async Task<EntryResult<AttachmentResponse>> UploadAsync(int userId, int entryId, string fileName, byte[] content, CancellationToken ct = default)
        {
            var (entry, error) = await LoadEntryForAccessAsync(userId, entryId, requireWrite: true, ct);
            if (entry is null)
            {
                return EntryResult<AttachmentResponse>.Fail(error);
            }

            if (string.IsNullOrWhiteSpace(fileName) ||
                !AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant()) ||
                content.Length == 0 || content.Length > _maxFileSizeBytes)
            {
                return EntryResult<AttachmentResponse>.Fail(EntryError.InvalidFile);
            }

            var attachment = new Attachment
            {
                PasswordEntryId = entryId,
                FileName = fileName,
                StoragePath = Guid.NewGuid().ToString("N"),
                FileSizeBytes = content.Length
            };

            await _storage.SaveAsync(attachment.StoragePath, _encryption.EncryptBytes(content), ct);
            _db.Attachments.Add(attachment);
            await _db.SaveChangesAsync(ct);

            return EntryResult<AttachmentResponse>.Ok(new AttachmentResponse
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FileSizeBytes = attachment.FileSizeBytes,
                CreatedAt = attachment.CreatedAt
            });
        }

        public async Task<EntryResult<(string FileName, byte[] Content)>> DownloadAsync(int userId, int entryId, int attachmentId, CancellationToken ct = default)
        {
            var (attachment, error) = await LoadAttachmentForAccessAsync(userId, entryId, attachmentId, requireWrite: false, ct);
            if (attachment is null)
            {
                return EntryResult<(string, byte[])>.Fail(error);
            }

            var encrypted = await _storage.ReadAsync(attachment.StoragePath, ct);
            if (encrypted is null)
            {
                return EntryResult<(string, byte[])>.Fail(EntryError.NotFound);
            }

            return EntryResult<(string, byte[])>.Ok((attachment.FileName, _encryption.DecryptBytes(encrypted)));
        }

        public async Task<EntryResult<bool>> DeleteAsync(int userId, int entryId, int attachmentId, CancellationToken ct = default)
        {
            var (attachment, error) = await LoadAttachmentForAccessAsync(userId, entryId, attachmentId, requireWrite: true, ct);
            if (attachment is null)
            {
                return EntryResult<bool>.Fail(error);
            }

            // Attachments aren't soft-deleted/trashed: the blob has no undo, so neither does the
            // row. TheShedContext.SaveChangesAsync only hard-deletes a Removed AuditableEntity
            // when it's already IsDeleted; pre-set it so this goes straight to a real DELETE
            // instead of the usual soft-delete-then-purge two-step.
            attachment.IsDeleted = true;
            _db.Attachments.Remove(attachment);
            await _db.SaveChangesAsync(ct);
            await _storage.DeleteAsync(attachment.StoragePath, ct);

            return EntryResult<bool>.Ok(true);
        }

        private async Task<(Attachment? Attachment, EntryError Error)> LoadAttachmentForAccessAsync(int userId, int entryId, int attachmentId, bool requireWrite, CancellationToken ct)
        {
            var (entry, entryError) = await LoadEntryForAccessAsync(userId, entryId, requireWrite, ct);
            if (entry is null)
            {
                return (null, entryError);
            }

            var attachment = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId && a.PasswordEntryId == entryId, ct);
            return attachment is null ? (null, EntryError.NotFound) : (attachment, EntryError.None);
        }

        // ponytail: same access-check shape as PasswordEntryService.LoadForAccessAsync; not
        // shared because that method lives on a sibling service, same call as SecureNoteService's copy.
        private async Task<(PasswordEntry? Entry, EntryError Error)> LoadEntryForAccessAsync(int userId, int entryId, bool requireWrite, CancellationToken ct)
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
    }
}
