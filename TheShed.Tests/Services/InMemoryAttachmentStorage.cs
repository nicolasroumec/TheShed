using TheShed.Server.Services;

namespace TheShed.Tests.Services
{
    /// <summary>In-memory IAttachmentStorage double: no real filesystem I/O in tests.</summary>
    internal sealed class InMemoryAttachmentStorage : IAttachmentStorage
    {
        private readonly Dictionary<string, byte[]> _files = new();
        public string? LastKey { get; private set; }

        public bool Contains(string key) => _files.ContainsKey(key);

        public Task SaveAsync(string key, byte[] content, CancellationToken ct = default)
        {
            _files[key] = content;
            LastKey = key;
            return Task.CompletedTask;
        }

        public Task<byte[]?> ReadAsync(string key, CancellationToken ct = default) =>
            Task.FromResult(_files.TryGetValue(key, out var content) ? content : null);

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            _files.Remove(key);
            return Task.CompletedTask;
        }
    }
}
