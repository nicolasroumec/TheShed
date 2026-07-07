using TheShed.Shared.Models.DTOs.Tags;

namespace TheShed.Server.Services
{
    public interface ITagService
    {
        Task<EntryResult<IReadOnlyList<TagResponse>>> ListAsync(int userId, CancellationToken ct = default);
        Task<EntryResult<TagResponse>> CreateAsync(int userId, TagCreateRequest request, CancellationToken ct = default);
        Task<EntryResult<TagResponse>> UpdateAsync(int userId, int tagId, TagUpdateRequest request, CancellationToken ct = default);
        Task<EntryResult<bool>> DeleteAsync(int userId, int tagId, CancellationToken ct = default);
    }
}
