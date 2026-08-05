using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Security;
using TheShed.Server.Services;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;
using Xunit;

namespace TheShed.Tests.Services
{
    public class AttachmentServiceTests
    {
        private static readonly string TestKey = Convert.ToBase64String(new byte[32]);
        private const int StrangerId = 9999; // a user with no access to the seeded vault
        private const long DefaultMaxSize = 5 * 1024 * 1024;

        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static (AttachmentService Service, InMemoryAttachmentStorage Storage) CreateService(TheShedContext db, long maxSize = DefaultMaxSize)
        {
            var encryption = new AesEncryptionService(Options.Create(new EncryptionSettings { Key = TestKey }));
            var storage = new InMemoryAttachmentStorage();
            var settings = Options.Create(new AttachmentSettings { MaxFileSizeBytes = maxSize });
            return (new AttachmentService(db, encryption, new VaultAccessService(db), storage, settings), storage);
        }

        private static async Task<(int ownerId, int vaultId, int entryId)> SeedEntryAsync(TheShedContext db)
        {
            var owner = new User { Username = "owner", Email = "owner@test.com", PasswordHash = "h" };
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var vault = new Vault { OwnerId = owner.Id, Name = "Personal" };
            db.Vaults.Add(vault);
            await db.SaveChangesAsync();

            var entry = new PasswordEntry { VaultId = vault.Id, Name = "Gmail", Username = "a", PasswordEncrypted = "x" };
            db.PasswordEntries.Add(entry);
            await db.SaveChangesAsync();

            return (owner.Id, vault.Id, entry.Id);
        }

        private static async Task<int> AddMemberAsync(TheShedContext db, int vaultId, VaultRole role)
        {
            var user = new User { Username = "member", Email = $"m{Guid.NewGuid():N}@test.com", PasswordHash = "h" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.VaultMembers.Add(new VaultMember { VaultId = vaultId, UserId = user.Id, Role = role });
            await db.SaveChangesAsync();

            return user.Id;
        }

        // --- Upload ---

        [Fact]
        public async Task UploadAsync_Owner_EncryptsAndStores()
        {
            using var db = CreateContext();
            var (ownerId, _, entryId) = await SeedEntryAsync(db);
            var (service, storage) = CreateService(db);
            var content = new byte[] { 1, 2, 3, 4 };

            var result = await service.UploadAsync(ownerId, entryId, "card.png", content);

            Assert.True(result.Success);
            Assert.Equal("card.png", result.Value!.FileName);
            Assert.Equal(content.Length, result.Value.FileSizeBytes);

            var stored = await storage.ReadAsync(storage.LastKey!);
            Assert.NotNull(stored);
            Assert.NotEqual(content, stored); // encrypted at rest, not the plaintext bytes
        }

        [Theory]
        [InlineData("virus.exe")]
        [InlineData("noextension")]
        public async Task UploadAsync_DisallowedExtension_ReturnsInvalidFile(string fileName)
        {
            using var db = CreateContext();
            var (ownerId, _, entryId) = await SeedEntryAsync(db);
            var (service, _) = CreateService(db);

            var result = await service.UploadAsync(ownerId, entryId, fileName, [1, 2, 3]);

            Assert.False(result.Success);
            Assert.Equal(EntryError.InvalidFile, result.Error);
        }

        [Fact]
        public async Task UploadAsync_EmptyContent_ReturnsInvalidFile()
        {
            using var db = CreateContext();
            var (ownerId, _, entryId) = await SeedEntryAsync(db);
            var (service, _) = CreateService(db);

            var result = await service.UploadAsync(ownerId, entryId, "notes.txt", []);

            Assert.False(result.Success);
            Assert.Equal(EntryError.InvalidFile, result.Error);
        }

        [Fact]
        public async Task UploadAsync_ExceedsMaxSize_ReturnsInvalidFile()
        {
            using var db = CreateContext();
            var (ownerId, _, entryId) = await SeedEntryAsync(db);
            var (service, _) = CreateService(db, maxSize: 4);

            var result = await service.UploadAsync(ownerId, entryId, "notes.txt", [1, 2, 3, 4, 5]);

            Assert.False(result.Success);
            Assert.Equal(EntryError.InvalidFile, result.Error);
        }

        [Fact]
        public async Task UploadAsync_ViewerMember_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (_, vaultId, entryId) = await SeedEntryAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var (service, _) = CreateService(db);

            var result = await service.UploadAsync(viewerId, entryId, "notes.txt", [1]);

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
        }

        [Fact]
        public async Task UploadAsync_NonMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (_, _, entryId) = await SeedEntryAsync(db);
            var (service, _) = CreateService(db);

            var result = await service.UploadAsync(StrangerId, entryId, "notes.txt", [1]);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        // --- List ---

        [Fact]
        public async Task ListAsync_ReturnsMetadataOrderedByFileName()
        {
            using var db = CreateContext();
            var (ownerId, _, entryId) = await SeedEntryAsync(db);
            var (service, _) = CreateService(db);
            await service.UploadAsync(ownerId, entryId, "z.txt", [1]);
            await service.UploadAsync(ownerId, entryId, "a.txt", [1]);

            var result = await service.ListAsync(ownerId, entryId);

            Assert.True(result.Success);
            Assert.Equal(["a.txt", "z.txt"], result.Value!.Select(a => a.FileName));
        }

        // --- Download ---

        [Fact]
        public async Task DownloadAsync_ReturnsDecryptedContent()
        {
            using var db = CreateContext();
            var (ownerId, _, entryId) = await SeedEntryAsync(db);
            var (service, _) = CreateService(db);
            var content = new byte[] { 10, 20, 30 };
            var uploaded = await service.UploadAsync(ownerId, entryId, "card.png", content);

            var result = await service.DownloadAsync(ownerId, entryId, uploaded.Value!.Id);

            Assert.True(result.Success);
            Assert.Equal("card.png", result.Value.FileName);
            Assert.Equal(content, result.Value.Content);
        }

        [Fact]
        public async Task DownloadAsync_ViewerMember_Allowed()
        {
            using var db = CreateContext();
            var (ownerId, vaultId, entryId) = await SeedEntryAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var (service, _) = CreateService(db);
            var uploaded = await service.UploadAsync(ownerId, entryId, "card.png", [1, 2, 3]);

            var result = await service.DownloadAsync(viewerId, entryId, uploaded.Value!.Id);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task DownloadAsync_NonMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (ownerId, _, entryId) = await SeedEntryAsync(db);
            var (service, _) = CreateService(db);
            var uploaded = await service.UploadAsync(ownerId, entryId, "card.png", [1, 2, 3]);

            var result = await service.DownloadAsync(StrangerId, entryId, uploaded.Value!.Id);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        // --- Delete ---

        [Fact]
        public async Task DeleteAsync_Owner_RemovesRowAndBlob()
        {
            using var db = CreateContext();
            var (ownerId, _, entryId) = await SeedEntryAsync(db);
            var (service, storage) = CreateService(db);
            var uploaded = await service.UploadAsync(ownerId, entryId, "card.png", [1, 2, 3]);
            var key = storage.LastKey!;

            var result = await service.DeleteAsync(ownerId, entryId, uploaded.Value!.Id);

            Assert.True(result.Success);
            Assert.Null(await db.Attachments.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == uploaded.Value.Id));
            Assert.Null(await storage.ReadAsync(key));
        }

        [Fact]
        public async Task DeleteAsync_ViewerMember_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (ownerId, vaultId, entryId) = await SeedEntryAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var (service, _) = CreateService(db);
            var uploaded = await service.UploadAsync(ownerId, entryId, "card.png", [1, 2, 3]);

            var result = await service.DeleteAsync(viewerId, entryId, uploaded.Value!.Id);

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
        }

        // In-memory IAttachmentStorage double: no real filesystem I/O in tests.
        private sealed class InMemoryAttachmentStorage : IAttachmentStorage
        {
            private readonly Dictionary<string, byte[]> _files = new();
            public string? LastKey { get; private set; }

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
}
