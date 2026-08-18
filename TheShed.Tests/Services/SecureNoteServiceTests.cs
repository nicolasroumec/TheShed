using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Notes;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;
using TheShed.Shared.Security;
using Xunit;

namespace TheShed.Tests.Services
{
    public class SecureNoteServiceTests
    {
        // A fixed 32-byte key keeps encryption deterministic across a test's operations.
        private static readonly byte[] TestKey = new byte[32];

        private const int StrangerId = 9999; // a user with no access to the seeded vault

        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static SecureNoteService CreateService(TheShedContext db)
        {
            var encryption = new AesEncryptionService(TestKey);
            return new SecureNoteService(db, encryption, new VaultAccessService(db));
        }

        private static async Task<(int ownerId, int vaultId)> SeedVaultAsync(TheShedContext db)
        {
            var owner = new User { Username = "owner", Email = "owner@test.com", PasswordHash = "h" };
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var vault = new Vault { OwnerId = owner.Id, Name = "Personal" };
            db.Vaults.Add(vault);
            await db.SaveChangesAsync();

            return (owner.Id, vault.Id);
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

        private static NoteCreateRequest SampleCreate(int vaultId) => new()
        {
            VaultId = vaultId,
            Title = "Recovery codes",
            Content = "top-secret-content",
            IsFavorite = true
        };

        [Fact]
        public async Task CreateAsync_Owner_EncryptsAtRestReturnsPlaintext()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            Assert.True(result.Success);
            Assert.Equal("top-secret-content", result.Value!.Content); // plaintext back to caller

            var stored = await db.SecureNotes.SingleAsync();
            Assert.NotEqual("top-secret-content", stored.ContentEncrypted); // never stored in plaintext
        }

        [Fact]
        public async Task CreateAsync_ViewerMember_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);

            var result = await service.CreateAsync(viewerId, SampleCreate(vaultId));

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
            Assert.Equal(0, await db.SecureNotes.CountAsync());
        }

        [Fact]
        public async Task GetAsync_NonMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.GetAsync(StrangerId, created.Value!.Id);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error); // existence not revealed
        }

        [Fact]
        public async Task GetAsync_ViewerMember_ReturnsDecryptedContent()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.GetAsync(viewerId, created.Value!.Id);

            Assert.True(result.Success);
            Assert.Equal("top-secret-content", result.Value!.Content);
        }

        [Fact]
        public async Task UpdateAsync_EditorMember_ReEncryptsContent()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.UpdateAsync(editorId, created.Value!.Id, new NoteUpdateRequest
            {
                Title = "Recovery codes",
                Content = "rotated-content",
                IsFavorite = false
            });

            Assert.True(result.Success);
            Assert.Equal("rotated-content", result.Value!.Content);

            var reread = await service.GetAsync(ownerId, created.Value!.Id);
            Assert.Equal("rotated-content", reread.Value!.Content);
        }

        [Fact]
        public async Task DeleteAsync_Owner_SoftDeletes()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.DeleteAsync(ownerId, created.Value!.Id);

            Assert.True(result.Success);
            Assert.Equal(0, await db.SecureNotes.CountAsync()); // filtered out by soft delete
            var afterDelete = await service.GetAsync(ownerId, created.Value!.Id);
            Assert.Equal(EntryError.NotFound, afterDelete.Error);
        }

        [Fact]
        public async Task DeleteAsync_ViewerMember_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.DeleteAsync(viewerId, created.Value!.Id);

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
            Assert.Equal(1, await db.SecureNotes.CountAsync());
        }
    }
}
