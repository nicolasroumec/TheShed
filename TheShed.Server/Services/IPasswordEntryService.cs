using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Entries;

namespace TheShed.Server.Services
{
    /// <summary>Result of an entry operation. On failure, <see cref="Value"/> is null.</summary>
    public record EntryResult<T>(bool Success, EntryError Error, T? Value)
    {
        public static EntryResult<T> Ok(T value) => new(true, EntryError.None, value);
        public static EntryResult<T> Fail(EntryError error) => new(false, error, default);
    }

    public interface IPasswordEntryService
    {
        Task<EntryResult<IReadOnlyList<EntryListItem>>> ListAsync(int userId, int vaultId, int? tagId = null, CancellationToken ct = default);
        Task<EntryResult<EntryResponse>> GetAsync(int userId, int entryId, CancellationToken ct = default);
        Task<EntryResult<EntryResponse>> CreateAsync(int userId, EntryCreateRequest request, CancellationToken ct = default);
        Task<EntryResult<EntryResponse>> UpdateAsync(int userId, int entryId, EntryUpdateRequest request, CancellationToken ct = default);
        Task<EntryResult<bool>> DeleteAsync(int userId, int entryId, CancellationToken ct = default);
        Task<EntryResult<bool>> AddTagAsync(int userId, int entryId, int tagId, CancellationToken ct = default);
        Task<EntryResult<bool>> RemoveTagAsync(int userId, int entryId, int tagId, CancellationToken ct = default);
        Task<EntryResult<bool>> SetFavoriteAsync(int userId, int entryId, bool isFavorite, CancellationToken ct = default);
    }
}
