using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;
using Xunit;

namespace TheShed.Tests.Services
{
    public class VaultServiceTests
    {
        private const int StrangerId = 9999; // a user with no access to the seeded vault

        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static VaultService CreateService(TheShedContext db) =>
            new(db, new VaultAccessService(db));

        private static async Task<int> AddUserAsync(TheShedContext db, string name)
        {
            var user = new User { Username = name, Email = $"{name}{Guid.NewGuid():N}@test.com", PasswordHash = "h" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        }

        // Seeds an owner + an owned vault, returns their ids.
        private static async Task<(int ownerId, int vaultId)> SeedVaultAsync(TheShedContext db, string name = "Personal")
        {
            var ownerId = await AddUserAsync(db, "owner");
            var vault = new Vault { OwnerId = ownerId, Name = name };
            db.Vaults.Add(vault);
            await db.SaveChangesAsync();
            return (ownerId, vault.Id);
        }

        private static async Task<int> AddMemberAsync(TheShedContext db, int vaultId, VaultRole role)
        {
            var userId = await AddUserAsync(db, "member");
            db.VaultMembers.Add(new VaultMember { VaultId = vaultId, UserId = userId, Role = role });
            await db.SaveChangesAsync();
            return userId;
        }

        // --- Create ---

        [Fact]
        public async Task CreateAsync_MakesCallerOwnerWithWriteAccess()
        {
            using var db = CreateContext();
            var userId = await AddUserAsync(db, "ana");
            var service = CreateService(db);

            var vault = await service.CreateAsync(userId, new VaultCreateRequest { Name = "  Work  ", Description = "stuff" });

            Assert.Equal("Work", vault.Name); // trimmed
            Assert.True(vault.IsOwner);
            Assert.True(vault.CanWrite);
            var stored = await db.Vaults.SingleAsync();
            Assert.Equal(userId, stored.OwnerId);
        }

        // --- List ---

        [Fact]
        public async Task ListAsync_ReturnsOwnedAndSharedOrderedByName()
        {
            using var db = CreateContext();
            var (ownerId, _) = await SeedVaultAsync(db, "Zeta");      // owned by ownerId
            var memberId = await AddUserAsync(db, "bob");

            // A second vault owned by someone else, where memberId is an Editor.
            var otherOwner = await AddUserAsync(db, "carl");
            var shared = new Vault { OwnerId = otherOwner, Name = "Alpha" };
            db.Vaults.Add(shared);
            await db.SaveChangesAsync();
            db.VaultMembers.Add(new VaultMember { VaultId = shared.Id, UserId = memberId, Role = VaultRole.Editor });
            // ownerId also owns "Zeta"; give ownerId a Viewer membership on "Alpha".
            db.VaultMembers.Add(new VaultMember { VaultId = shared.Id, UserId = ownerId, Role = VaultRole.Viewer });
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var list = await service.ListAsync(ownerId);

            Assert.Collection(list,
                first =>
                {
                    Assert.Equal("Alpha", first.Name); // shared via Viewer membership
                    Assert.False(first.IsOwner);
                    Assert.False(first.CanWrite);
                },
                second =>
                {
                    Assert.Equal("Zeta", second.Name); // owned
                    Assert.True(second.IsOwner);
                    Assert.True(second.CanWrite);
                });
        }

        [Fact]
        public async Task ListAsync_EditorMember_HasWriteButNotOwner()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            var service = CreateService(db);

            var item = Assert.Single(await service.ListAsync(editorId));

            Assert.False(item.IsOwner);
            Assert.True(item.CanWrite);
        }

        [Fact]
        public async Task ListAsync_Stranger_SeesNothing()
        {
            using var db = CreateContext();
            await SeedVaultAsync(db);
            var service = CreateService(db);

            Assert.Empty(await service.ListAsync(StrangerId));
        }

        // --- Get ---

        [Fact]
        public async Task GetAsync_ViewerMember_CanReadButNotWrite()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);

            var result = await service.GetAsync(viewerId, vaultId);

            Assert.True(result.Success);
            Assert.False(result.Value!.IsOwner);
            Assert.False(result.Value!.CanWrite);
        }

        [Fact]
        public async Task GetAsync_Stranger_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.GetAsync(StrangerId, vaultId);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error); // existence not revealed
        }

        // --- Update ---

        [Fact]
        public async Task UpdateAsync_Owner_RenamesVault()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.UpdateAsync(ownerId, vaultId, new VaultUpdateRequest { Name = "Renamed", Description = "d" });

            Assert.True(result.Success);
            Assert.Equal("Renamed", result.Value!.Name);
            Assert.Equal("Renamed", (await db.Vaults.SingleAsync()).Name);
        }

        [Fact]
        public async Task UpdateAsync_EditorMember_ReturnsForbidden()
        {
            // Editing the vault itself is owner-only; an Editor manages entries, not the vault.
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            var service = CreateService(db);

            var result = await service.UpdateAsync(editorId, vaultId, new VaultUpdateRequest { Name = "Hijack" });

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
        }

        [Fact]
        public async Task UpdateAsync_Stranger_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.UpdateAsync(StrangerId, vaultId, new VaultUpdateRequest { Name = "x" });

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        // --- Delete ---

        [Fact]
        public async Task DeleteAsync_Owner_SoftDeletes()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.DeleteAsync(ownerId, vaultId);

            Assert.True(result.Success);
            Assert.Equal(0, await db.Vaults.CountAsync()); // filtered out by soft delete
            Assert.Equal(EntryError.NotFound, (await service.GetAsync(ownerId, vaultId)).Error);
        }

        [Fact]
        public async Task DeleteAsync_EditorMember_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            var service = CreateService(db);

            var result = await service.DeleteAsync(editorId, vaultId);

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
            Assert.Equal(1, await db.Vaults.CountAsync());
        }
    }
}
