using TheShed.Shared.Models.DTOs.Trash;
using TheShed.Shared.Models.Enums;

namespace TheShed.Server.Services
{
    public interface ITrashService
    {
        Task<IReadOnlyList<TrashItem>> ListAsync(int userId, CancellationToken ct = default);
        Task<EntryResult<bool>> RestoreAsync(int userId, TrashItemType type, int id, CancellationToken ct = default);
        Task<EntryResult<bool>> PurgeAsync(int userId, TrashItemType type, int id, CancellationToken ct = default);

        /// <summary>Hard-deletes every soft-deleted row (any user) past the retention window as of <paramref name="now"/>.</summary>
        Task<int> PurgeExpiredAsync(DateTime now, CancellationToken ct = default);
    }
}
