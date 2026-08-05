namespace TheShed.Server.Services
{
    /// <summary>Stores opaque attachment bytes (already encrypted by the caller) under a key.
    /// Minimal on purpose so a future blob-storage backend is a drop-in swap.</summary>
    public interface IAttachmentStorage
    {
        Task SaveAsync(string key, byte[] content, CancellationToken ct = default);
        Task<byte[]?> ReadAsync(string key, CancellationToken ct = default);
        Task DeleteAsync(string key, CancellationToken ct = default);
    }
}
