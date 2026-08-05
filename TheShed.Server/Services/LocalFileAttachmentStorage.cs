using Microsoft.Extensions.Options;

namespace TheShed.Server.Services
{
    /// <summary>Dev/local <see cref="IAttachmentStorage"/>: one file per key under <see cref="AttachmentSettings.StoragePath"/>.</summary>
    public class LocalFileAttachmentStorage : IAttachmentStorage
    {
        private readonly string _root;

        public LocalFileAttachmentStorage(IOptions<AttachmentSettings> settings)
        {
            _root = Path.GetFullPath(settings.Value.StoragePath);
            Directory.CreateDirectory(_root);
        }

        public Task SaveAsync(string key, byte[] content, CancellationToken ct = default) =>
            File.WriteAllBytesAsync(PathFor(key), content, ct);

        public async Task<byte[]?> ReadAsync(string key, CancellationToken ct = default)
        {
            var path = PathFor(key);
            return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
        }

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            File.Delete(PathFor(key));
            return Task.CompletedTask;
        }

        // ponytail: key is always a server-generated Guid (see AttachmentService), never
        // user input, so there's no path-traversal surface to sanitize here.
        private string PathFor(string key) => Path.Combine(_root, key);
    }
}
