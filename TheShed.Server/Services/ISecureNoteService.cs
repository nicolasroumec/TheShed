using TheShed.Shared.Models.DTOs.Notes;

namespace TheShed.Server.Services
{
    public interface ISecureNoteService
    {
        Task<EntryResult<IReadOnlyList<NoteListItem>>> ListAsync(int userId, int vaultId, CancellationToken ct = default);
        Task<EntryResult<NoteResponse>> GetAsync(int userId, int noteId, CancellationToken ct = default);
        Task<EntryResult<NoteResponse>> CreateAsync(int userId, NoteCreateRequest request, CancellationToken ct = default);
        Task<EntryResult<NoteResponse>> UpdateAsync(int userId, int noteId, NoteUpdateRequest request, CancellationToken ct = default);
        Task<EntryResult<bool>> DeleteAsync(int userId, int noteId, CancellationToken ct = default);
    }
}
