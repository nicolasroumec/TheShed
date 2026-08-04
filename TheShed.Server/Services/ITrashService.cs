using TheShed.Shared.Models.DTOs.Trash;
using TheShed.Shared.Models.Enums;

namespace TheShed.Server.Services
{
    public interface ITrashService
    {
        Task<IReadOnlyList<TrashItem>> ListAsync(int userId, CancellationToken ct = default);
        Task<EntryResult<bool>> RestoreAsync(int userId, TrashItemType type, int id, CancellationToken ct = default);
        Task<EntryResult<bool>> PurgeAsync(int userId, TrashItemType type, int id, CancellationToken ct = default);
    }
}
