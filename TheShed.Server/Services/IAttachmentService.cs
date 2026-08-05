using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Attachments;

namespace TheShed.Server.Services
{
    public interface IAttachmentService
    {
        Task<EntryResult<IReadOnlyList<AttachmentResponse>>> ListAsync(int userId, int entryId, CancellationToken ct = default);
        Task<EntryResult<AttachmentResponse>> UploadAsync(int userId, int entryId, string fileName, byte[] content, CancellationToken ct = default);
        Task<EntryResult<(string FileName, byte[] Content)>> DownloadAsync(int userId, int entryId, int attachmentId, CancellationToken ct = default);
        Task<EntryResult<bool>> DeleteAsync(int userId, int entryId, int attachmentId, CancellationToken ct = default);
    }
}
