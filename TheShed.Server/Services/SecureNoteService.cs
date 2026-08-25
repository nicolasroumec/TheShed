using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Notes;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Services
{
    public class SecureNoteService : ISecureNoteService
    {
        private readonly TheShedContext _db;
        private readonly IVaultAccessService _access;

        public SecureNoteService(TheShedContext db, IVaultAccessService access)
        {
            _db = db;
            _access = access;
        }

        public async Task<EntryResult<IReadOnlyList<NoteListItem>>> ListAsync(int userId, int vaultId, CancellationToken ct = default)
        {
            var access = await _access.GetAccessAsync(vaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                // Hide the existence of vaults the user cannot see.
                return EntryResult<IReadOnlyList<NoteListItem>>.Fail(EntryError.NotFound);
            }

            // Title is ciphertext (Sprint 26) — the server can no longer sort by it meaningfully.
            // It returns notes ordered by Id; the caller decrypts and re-sorts favorite-then-title
            // client-side, same as PasswordEntryService.
            var items = await _db.SecureNotes
                .Where(n => n.VaultId == vaultId)
                .OrderBy(n => n.Id)
                .Select(n => new NoteListItem
                {
                    Id = n.Id,
                    Title = n.Title,
                    IsFavorite = n.IsFavorite
                })
                .ToListAsync(ct);

            return EntryResult<IReadOnlyList<NoteListItem>>.Ok(items);
        }

        public async Task<EntryResult<NoteResponse>> GetAsync(int userId, int noteId, CancellationToken ct = default)
        {
            var note = await _db.SecureNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);
            if (note is null)
            {
                return EntryResult<NoteResponse>.Fail(EntryError.NotFound);
            }

            var access = await _access.GetAccessAsync(note.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryResult<NoteResponse>.Fail(EntryError.NotFound);
            }

            return EntryResult<NoteResponse>.Ok(ToResponse(note));
        }

        public async Task<EntryResult<NoteResponse>> CreateAsync(int userId, NoteCreateRequest request, CancellationToken ct = default)
        {
            var access = await _access.GetAccessAsync(request.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryResult<NoteResponse>.Fail(EntryError.NotFound);
            }
            if (access != VaultAccess.Write)
            {
                return EntryResult<NoteResponse>.Fail(EntryError.Forbidden);
            }

            var note = new SecureNote
            {
                VaultId = request.VaultId,
                Title = request.Title.Trim(),
                ContentEncrypted = request.Content,
                IsFavorite = request.IsFavorite
            };

            _db.SecureNotes.Add(note);
            await _db.SaveChangesAsync(ct);

            return EntryResult<NoteResponse>.Ok(ToResponse(note));
        }

        public async Task<EntryResult<NoteResponse>> UpdateAsync(int userId, int noteId, NoteUpdateRequest request, CancellationToken ct = default)
        {
            var note = await _db.SecureNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);
            if (note is null)
            {
                return EntryResult<NoteResponse>.Fail(EntryError.NotFound);
            }

            var access = await _access.GetAccessAsync(note.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryResult<NoteResponse>.Fail(EntryError.NotFound);
            }
            if (access != VaultAccess.Write)
            {
                return EntryResult<NoteResponse>.Fail(EntryError.Forbidden);
            }

            note.Title = request.Title.Trim();
            note.ContentEncrypted = request.Content;
            note.IsFavorite = request.IsFavorite;

            await _db.SaveChangesAsync(ct);

            return EntryResult<NoteResponse>.Ok(ToResponse(note));
        }

        public async Task<EntryResult<bool>> DeleteAsync(int userId, int noteId, CancellationToken ct = default)
        {
            var note = await _db.SecureNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);
            if (note is null)
            {
                return EntryResult<bool>.Fail(EntryError.NotFound);
            }

            var access = await _access.GetAccessAsync(note.VaultId, userId, ct);
            if (access == VaultAccess.None)
            {
                return EntryResult<bool>.Fail(EntryError.NotFound);
            }
            if (access != VaultAccess.Write)
            {
                return EntryResult<bool>.Fail(EntryError.Forbidden);
            }

            // Soft delete: the SaveChanges override turns this into an IsDeleted update.
            _db.SecureNotes.Remove(note);
            await _db.SaveChangesAsync(ct);

            return EntryResult<bool>.Ok(true);
        }

        /// <summary>Maps a note to its detail DTO. Title and Content are passed through as
        /// stored — the server cannot decrypt either of them (Sprint 26).</summary>
        private NoteResponse ToResponse(SecureNote note) => new()
        {
            Id = note.Id,
            VaultId = note.VaultId,
            Title = note.Title,
            Content = note.ContentEncrypted,
            IsFavorite = note.IsFavorite,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }
}
