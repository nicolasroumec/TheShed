using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Security;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;
using Xunit;

namespace TheShed.Tests.Services
{
    public class PasswordEntryServiceTests
    {
        // A fixed 32-byte key keeps encryption deterministic across a test's operations.
        private static readonly byte[] TestKey = new byte[32];

        private const int StrangerId = 9999; // a user with no access to the seeded vault

        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static PasswordEntryService CreateService(TheShedContext db)
        {
            var encryption = new AesEncryptionService(TestKey);
            return new PasswordEntryService(db, encryption, new VaultAccessService(db));
        }

        // Seeds a user (owner) + an owned vault, returns their ids.
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

        // Adds a member user to a vault with the given role, returns the user id.
        private static async Task<int> AddMemberAsync(TheShedContext db, int vaultId, VaultRole role)
        {
            var user = new User { Username = "member", Email = $"m{Guid.NewGuid():N}@test.com", PasswordHash = "h" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.VaultMembers.Add(new VaultMember { VaultId = vaultId, UserId = user.Id, Role = role });
            await db.SaveChangesAsync();

            return user.Id;
        }

        private static EntryCreateRequest SampleCreate(int vaultId) => new()
        {
            VaultId = vaultId,
            Name = "Gmail",
            Username = "ana@gmail.com",
            Password = "super-secret",
            Url = "https://gmail.com",
            IsFavorite = true
        };

        // --- Create ---

        [Fact]
        public async Task CreateAsync_Owner_EncryptsAndReturnsPlaintext()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            Assert.True(result.Success);
            Assert.Equal("super-secret", result.Value!.Password); // plaintext back to caller

            var stored = await db.PasswordEntries.SingleAsync();
            Assert.NotEqual("super-secret", stored.PasswordEncrypted); // never stored in plaintext
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
            Assert.Equal(0, await db.PasswordEntries.CountAsync());
        }

        [Fact]
        public async Task CreateAsync_NonMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.CreateAsync(StrangerId, SampleCreate(vaultId));

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error); // existence not revealed
        }

        // --- Get ---

        [Fact]
        public async Task GetAsync_Owner_ReturnsDecryptedPassword()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.GetAsync(ownerId, created.Value!.Id);

            Assert.True(result.Success);
            Assert.Equal("super-secret", result.Value!.Password);
        }

        [Fact]
        public async Task GetAsync_ViewerMember_CanRead()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.GetAsync(viewerId, created.Value!.Id);

            Assert.True(result.Success);
            Assert.Equal("super-secret", result.Value!.Password);
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
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        // --- List ---

        [Fact]
        public async Task ListAsync_ReturnsMetadataOrderedByName()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            await service.CreateAsync(ownerId, new EntryCreateRequest { VaultId = vaultId, Name = "Zelda", Username = "z", Password = "p" });
            await service.CreateAsync(ownerId, new EntryCreateRequest { VaultId = vaultId, Name = "Amazon", Username = "a", Password = "p" });

            var result = await service.ListAsync(ownerId, vaultId);

            Assert.True(result.Success);
            Assert.Collection(result.Value!,
                first => Assert.Equal("Amazon", first.Name),
                second => Assert.Equal("Zelda", second.Name));
        }

        [Fact]
        public async Task ListAsync_NonMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.ListAsync(StrangerId, vaultId);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        [Fact]
        public async Task ListAsync_FavoritesSortFirst()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            await service.CreateAsync(ownerId, new EntryCreateRequest { VaultId = vaultId, Name = "Amazon", Username = "a", Password = "p" });
            await service.CreateAsync(ownerId, new EntryCreateRequest { VaultId = vaultId, Name = "Zelda", Username = "z", Password = "p", IsFavorite = true });

            var result = await service.ListAsync(ownerId, vaultId);

            Assert.Collection(result.Value!,
                first => Assert.Equal("Zelda", first.Name),   // favorite, despite sorting after "Amazon" alphabetically
                second => Assert.Equal("Amazon", second.Name));
        }

        [Fact]
        public async Task ListAsync_Search_FiltersByNameUsernameOrUrl()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            await service.CreateAsync(ownerId, new EntryCreateRequest { VaultId = vaultId, Name = "Gmail", Username = "ana", Password = "p", Url = "https://gmail.com" });
            await service.CreateAsync(ownerId, new EntryCreateRequest { VaultId = vaultId, Name = "Amazon", Username = "shopper", Password = "p", Url = "https://amazon.com" });

            var byName = await service.ListAsync(ownerId, vaultId, search: "gmail");
            var byUsername = await service.ListAsync(ownerId, vaultId, search: "shopper");
            var byUrl = await service.ListAsync(ownerId, vaultId, search: "amazon.com");
            var noMatch = await service.ListAsync(ownerId, vaultId, search: "nope");

            Assert.Equal("Gmail", Assert.Single(byName.Value!).Name);
            Assert.Equal("Amazon", Assert.Single(byUsername.Value!).Name);
            Assert.Equal("Amazon", Assert.Single(byUrl.Value!).Name);
            Assert.Empty(noMatch.Value!);
        }

        // --- Update ---

        [Fact]
        public async Task UpdateAsync_EditorMember_ReEncryptsPassword()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.UpdateAsync(editorId, created.Value!.Id, new EntryUpdateRequest
            {
                Name = "Gmail",
                Username = "ana@gmail.com",
                Password = "new-secret",
                IsFavorite = false
            });

            Assert.True(result.Success);
            Assert.Equal("new-secret", result.Value!.Password);

            var reread = await service.GetAsync(ownerId, created.Value!.Id);
            Assert.Equal("new-secret", reread.Value!.Password);
        }

        [Fact]
        public async Task UpdateAsync_ViewerMember_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.UpdateAsync(viewerId, created.Value!.Id, new EntryUpdateRequest
            {
                Name = "x",
                Username = "x",
                Password = "x"
            });

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
        }

        // --- Delete ---

        [Fact]
        public async Task DeleteAsync_Owner_SoftDeletes()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.DeleteAsync(ownerId, created.Value!.Id);

            Assert.True(result.Success);
            Assert.Equal(0, await db.PasswordEntries.CountAsync()); // filtered out by soft delete
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
            Assert.Equal(1, await db.PasswordEntries.CountAsync());
        }

        // --- SetFavorite ---

        [Fact]
        public async Task SetFavoriteAsync_EditorMember_TogglesFlag()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, new EntryCreateRequest { VaultId = vaultId, Name = "Gmail", Username = "a", Password = "p", IsFavorite = false });

            var result = await service.SetFavoriteAsync(editorId, created.Value!.Id, true);

            Assert.True(result.Success);
            var reread = await service.GetAsync(ownerId, created.Value!.Id);
            Assert.True(reread.Value!.IsFavorite);
        }

        [Fact]
        public async Task SetFavoriteAsync_SameValue_IsNoopButSucceeds()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId)); // IsFavorite = true

            var result = await service.SetFavoriteAsync(ownerId, created.Value!.Id, true);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task SetFavoriteAsync_ViewerMember_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.SetFavoriteAsync(viewerId, created.Value!.Id, true);

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
        }

        [Fact]
        public async Task SetFavoriteAsync_NonMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.SetFavoriteAsync(StrangerId, created.Value!.Id, true);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        // --- History ---

        [Fact]
        public async Task UpdateAsync_PasswordChanged_SnapshotsOutgoingPassword()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId)); // password: super-secret

            await service.UpdateAsync(ownerId, created.Value!.Id, new EntryUpdateRequest
            {
                Name = "Gmail",
                Username = "ana@gmail.com",
                Password = "new-secret",
                IsFavorite = false
            });

            var history = await service.GetHistoryAsync(ownerId, created.Value!.Id);
            var item = Assert.Single(history.Value!);
            Assert.Equal(ownerId, (await db.EntryHistory.FirstAsync()).ChangedByUserId);

            var detail = await service.GetHistoryEntryAsync(ownerId, created.Value!.Id, item.Id);
            Assert.Equal("super-secret", detail.Value!.Password); // the old password, decrypted
        }

        [Fact]
        public async Task UpdateAsync_PasswordUnchanged_DoesNotSnapshot()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId)); // password: super-secret

            await service.UpdateAsync(ownerId, created.Value!.Id, new EntryUpdateRequest
            {
                Name = "Gmail renamed",
                Username = "ana@gmail.com",
                Password = "super-secret", // unchanged
                IsFavorite = false
            });

            var history = await service.GetHistoryAsync(ownerId, created.Value!.Id);
            Assert.Empty(history.Value!);
        }

        [Fact]
        public async Task GetHistoryAsync_ReturnsNewestFirst()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId)); // password: super-secret

            await service.UpdateAsync(ownerId, created.Value!.Id, new EntryUpdateRequest { Name = "Gmail", Username = "ana@gmail.com", Password = "second-secret" });
            await service.UpdateAsync(ownerId, created.Value!.Id, new EntryUpdateRequest { Name = "Gmail", Username = "ana@gmail.com", Password = "third-secret" });

            var history = await service.GetHistoryAsync(ownerId, created.Value!.Id);

            Assert.Equal(2, history.Value!.Count);
            Assert.True(history.Value![0].CreatedAt >= history.Value![1].CreatedAt);
        }

        [Fact]
        public async Task GetHistoryAsync_NonMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var result = await service.GetHistoryAsync(StrangerId, created.Value!.Id);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        [Fact]
        public async Task GetHistoryEntryAsync_ViewerMember_CanRead()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));
            await service.UpdateAsync(ownerId, created.Value!.Id, new EntryUpdateRequest { Name = "Gmail", Username = "ana@gmail.com", Password = "new-secret" });
            var historyId = (await service.GetHistoryAsync(ownerId, created.Value!.Id)).Value!.Single().Id;

            var result = await service.GetHistoryEntryAsync(viewerId, created.Value!.Id, historyId);

            Assert.True(result.Success);
            Assert.Equal("super-secret", result.Value!.Password);
        }

        [Fact]
        public async Task GetHistoryEntryAsync_HistoryFromAnotherEntry_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var entryA = await service.CreateAsync(ownerId, SampleCreate(vaultId));
            var entryB = await service.CreateAsync(ownerId, new EntryCreateRequest { VaultId = vaultId, Name = "Other", Username = "b", Password = "b-pass" });
            await service.UpdateAsync(ownerId, entryA.Value!.Id, new EntryUpdateRequest { Name = "Gmail", Username = "ana@gmail.com", Password = "new-secret" });
            var historyId = (await service.GetHistoryAsync(ownerId, entryA.Value!.Id)).Value!.Single().Id;

            var result = await service.GetHistoryEntryAsync(ownerId, entryB.Value!.Id, historyId);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        [Fact]
        public async Task PasswordChangedAt_DefaultsToEntryCreation_WhenNeverChanged()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var listed = await service.ListAsync(ownerId, vaultId);
            var got = await service.GetAsync(ownerId, created.Value!.Id);

            Assert.Equal(created.Value!.CreatedAt, listed.Value!.Single().PasswordChangedAt);
            Assert.Equal(created.Value!.CreatedAt, got.Value!.PasswordChangedAt);
        }

        [Fact]
        public async Task PasswordChangedAt_ReflectsLatestSnapshot_AfterPasswordChange()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);
            var created = await service.CreateAsync(ownerId, SampleCreate(vaultId));

            var updated = await service.UpdateAsync(ownerId, created.Value!.Id, new EntryUpdateRequest
            {
                Name = "Gmail",
                Username = "ana@gmail.com",
                Password = "new-secret"
            });

            var historySnapshot = (await service.GetHistoryAsync(ownerId, created.Value!.Id)).Value!.Single();
            Assert.Equal(historySnapshot.CreatedAt, updated.Value!.PasswordChangedAt);
            Assert.NotEqual(created.Value!.CreatedAt, updated.Value!.PasswordChangedAt);
        }
    }
}
