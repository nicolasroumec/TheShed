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

            var vault = await service.CreateAsync(userId,
                new VaultCreateRequest { Name = "  Work  ", Description = "stuff", VaultKeyWrap = "wrapped-key" });

            Assert.Equal("Work", vault.Name); // trimmed
            Assert.True(vault.IsOwner);
            Assert.True(vault.CanWrite);
            Assert.Equal("wrapped-key", vault.WrappedKey);
            var stored = await db.Vaults.SingleAsync();
            Assert.Equal(userId, stored.OwnerId);
        }

        [Fact]
        public async Task CreateAsync_StoresVaultKeyWrapForOwner()
        {
            using var db = CreateContext();
            var userId = await AddUserAsync(db, "ana");
            var service = CreateService(db);

            var vault = await service.CreateAsync(userId,
                new VaultCreateRequest { Name = "Work", VaultKeyWrap = "wrapped-key" });

            var wrap = await db.VaultKeyWraps.SingleAsync();
            Assert.Equal(vault.Id, wrap.VaultId);
            Assert.Equal(userId, wrap.UserId);
            Assert.Equal("wrapped-key", wrap.WrappedKey);
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
        public async Task GetAsync_Owner_ReturnsWrappedKey()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            db.VaultKeyWraps.Add(new VaultKeyWrap { VaultId = vaultId, UserId = ownerId, WrappedKey = "wrapped-key" });
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.GetAsync(ownerId, vaultId);

            Assert.True(result.Success);
            Assert.Equal("wrapped-key", result.Value!.WrappedKey);
        }

        [Fact]
        public async Task GetAsync_ViewerMember_NoWrapYet_ReturnsNullWrappedKey()
        {
            // A shared member has no VaultKeyWrap row until Sprint 27 (key wrapping via RSA).
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);

            var result = await service.GetAsync(viewerId, vaultId);

            Assert.True(result.Success);
            Assert.Null(result.Value!.WrappedKey);
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
        public async Task UpdateAsync_Owner_ReturnsWrappedKey()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            db.VaultKeyWraps.Add(new VaultKeyWrap { VaultId = vaultId, UserId = ownerId, WrappedKey = "wrapped-key" });
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.UpdateAsync(ownerId, vaultId, new VaultUpdateRequest { Name = "Renamed" });

            Assert.True(result.Success);
            Assert.Equal("wrapped-key", result.Value!.WrappedKey);
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

        // --- Members ---

        // Adds a user with a known email (AddUserAsync randomizes it).
        private static async Task<int> AddUserWithEmailAsync(TheShedContext db, string name, string email)
        {
            var user = new User { Username = name, Email = email, PasswordHash = "h" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        }

        [Fact]
        public async Task AddMemberAsync_Owner_SharesByEmail()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var targetId = await AddUserWithEmailAsync(db, "bob", "bob@test.com");
            var service = CreateService(db);

            var result = await service.AddMemberAsync(ownerId, vaultId,
                new VaultMemberAddRequest { Email = "bob@test.com", Role = VaultRole.Editor, VaultKeyWrap = "rsa-wrapped-key" });

            Assert.True(result.Success);
            Assert.Equal(targetId, result.Value!.UserId);
            Assert.Equal(VaultRole.Editor, result.Value!.Role);
            Assert.True(await db.VaultMembers.AnyAsync(m => m.VaultId == vaultId && m.UserId == targetId));
        }

        [Fact]
        public async Task AddMemberAsync_Owner_PersistsVaultKeyWrapForTarget()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var targetId = await AddUserWithEmailAsync(db, "bob", "bob@test.com");
            var service = CreateService(db);

            await service.AddMemberAsync(ownerId, vaultId,
                new VaultMemberAddRequest { Email = "bob@test.com", VaultKeyWrap = "rsa-wrapped-key" });

            var wrap = await db.VaultKeyWraps.SingleAsync(w => w.VaultId == vaultId && w.UserId == targetId);
            Assert.Equal("rsa-wrapped-key", wrap.WrappedKey);
        }

        [Fact]
        public async Task AddMemberAsync_UnknownEmail_ReturnsUserNotFound()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.AddMemberAsync(ownerId, vaultId,
                new VaultMemberAddRequest { Email = "nobody@test.com" });

            Assert.Equal(EntryError.UserNotFound, result.Error);
        }

        [Fact]
        public async Task AddMemberAsync_AlreadyMember_ReturnsAlreadyMember()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var targetId = await AddUserWithEmailAsync(db, "bob", "bob@test.com");
            db.VaultMembers.Add(new VaultMember { VaultId = vaultId, UserId = targetId, Role = VaultRole.Viewer });
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.AddMemberAsync(ownerId, vaultId,
                new VaultMemberAddRequest { Email = "bob@test.com" });

            Assert.Equal(EntryError.AlreadyMember, result.Error);
        }

        [Fact]
        public async Task AddMemberAsync_NonOwner_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            await AddUserWithEmailAsync(db, "bob", "bob@test.com");
            var service = CreateService(db);

            var result = await service.AddMemberAsync(editorId, vaultId,
                new VaultMemberAddRequest { Email = "bob@test.com" });

            Assert.Equal(EntryError.Forbidden, result.Error);
        }

        [Fact]
        public async Task ListMembersAsync_Owner_ReturnsMembers()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);

            var result = await service.ListMembersAsync(ownerId, vaultId);

            Assert.True(result.Success);
            Assert.Single(result.Value!);
        }

        [Fact]
        public async Task UpdateMemberRoleAsync_Owner_ChangesRole()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var memberId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);

            var result = await service.UpdateMemberRoleAsync(ownerId, vaultId, memberId,
                new VaultMemberRoleUpdateRequest { Role = VaultRole.Editor });

            Assert.True(result.Success);
            Assert.Equal(VaultRole.Editor, result.Value!.Role);
            Assert.Equal(VaultRole.Editor, (await db.VaultMembers.SingleAsync()).Role);
        }

        [Fact]
        public async Task UpdateMemberRoleAsync_NotAMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var service = CreateService(db);

            var result = await service.UpdateMemberRoleAsync(ownerId, vaultId, StrangerId,
                new VaultMemberRoleUpdateRequest { Role = VaultRole.Editor });

            Assert.Equal(EntryError.NotFound, result.Error);
        }

        [Fact]
        public async Task RemoveMemberAsync_Owner_RemovesMembership()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var memberId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);

            var result = await service.RemoveMemberAsync(ownerId, vaultId, memberId);

            Assert.True(result.Success);
            Assert.Equal(0, await db.VaultMembers.CountAsync());
        }

        [Fact]
        public async Task RemoveMemberAsync_Owner_RemovesTheMembersVaultKeyWrap()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var memberId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            db.VaultKeyWraps.Add(new VaultKeyWrap { VaultId = vaultId, UserId = memberId, WrappedKey = "rsa-wrapped-key" });
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.RemoveMemberAsync(ownerId, vaultId, memberId);

            Assert.True(result.Success);
            Assert.False(await db.VaultKeyWraps.AnyAsync(w => w.VaultId == vaultId && w.UserId == memberId));
        }

        [Fact]
        public async Task RemoveMemberAsync_NonOwner_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            var service = CreateService(db);

            var result = await service.RemoveMemberAsync(editorId, vaultId, 12345);

            Assert.Equal(EntryError.Forbidden, result.Error);
        }
    }
}
