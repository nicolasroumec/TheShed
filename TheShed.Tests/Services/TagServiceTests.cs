using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Tags;
using TheShed.Shared.Models.Entities;
using Xunit;

namespace TheShed.Tests.Services
{
    public class TagServiceTests
    {
        private const int UserId = 1;
        private const int OtherUserId = 2;

        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        // Seeds a tag directly and returns its id.
        private static async Task<int> SeedTagAsync(TheShedContext db, int userId, string name)
        {
            var tag = new Tag { UserId = userId, Name = name };
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
            return tag.Id;
        }

        // --- Create ---

        [Fact]
        public async Task CreateAsync_NewName_PersistsTrimmed()
        {
            using var db = CreateContext();
            var service = new TagService(db);

            var result = await service.CreateAsync(UserId, new TagCreateRequest { Name = "  Work  " });

            Assert.True(result.Success);
            Assert.Equal("Work", result.Value!.Name);
            var stored = await db.Tags.SingleAsync();
            Assert.Equal("Work", stored.Name);
            Assert.Equal(UserId, stored.UserId);
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_ReturnsConflict()
        {
            using var db = CreateContext();
            await SeedTagAsync(db, UserId, "Work");
            var service = new TagService(db);

            var result = await service.CreateAsync(UserId, new TagCreateRequest { Name = "Work" });

            Assert.False(result.Success);
            Assert.Equal(EntryError.Conflict, result.Error);
            Assert.Equal(1, await db.Tags.CountAsync());
        }

        [Fact]
        public async Task CreateAsync_SameNameDifferentUser_Allowed()
        {
            using var db = CreateContext();
            await SeedTagAsync(db, OtherUserId, "Work");
            var service = new TagService(db);

            var result = await service.CreateAsync(UserId, new TagCreateRequest { Name = "Work" });

            Assert.True(result.Success); // uniqueness is per-user
            Assert.Equal(2, await db.Tags.CountAsync());
        }

        // --- List ---

        [Fact]
        public async Task ListAsync_ReturnsOnlyOwnTagsOrderedByName()
        {
            using var db = CreateContext();
            await SeedTagAsync(db, UserId, "Zeta");
            await SeedTagAsync(db, UserId, "Alpha");
            await SeedTagAsync(db, OtherUserId, "Other");
            var service = new TagService(db);

            var result = await service.ListAsync(UserId);

            Assert.True(result.Success);
            Assert.Collection(result.Value!,
                first => Assert.Equal("Alpha", first.Name),
                second => Assert.Equal("Zeta", second.Name));
        }

        // --- Update ---

        [Fact]
        public async Task UpdateAsync_Owned_Renames()
        {
            using var db = CreateContext();
            var tagId = await SeedTagAsync(db, UserId, "Work");
            var service = new TagService(db);

            var result = await service.UpdateAsync(UserId, tagId, new TagUpdateRequest { Name = "Job" });

            Assert.True(result.Success);
            Assert.Equal("Job", result.Value!.Name);
            Assert.Equal("Job", (await db.Tags.SingleAsync()).Name);
        }

        [Fact]
        public async Task UpdateAsync_NotOwned_ReturnsNotFound()
        {
            using var db = CreateContext();
            var tagId = await SeedTagAsync(db, OtherUserId, "Work");
            var service = new TagService(db);

            var result = await service.UpdateAsync(UserId, tagId, new TagUpdateRequest { Name = "Job" });

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        [Fact]
        public async Task UpdateAsync_NameClashWithAnotherTag_ReturnsConflict()
        {
            using var db = CreateContext();
            await SeedTagAsync(db, UserId, "Personal");
            var tagId = await SeedTagAsync(db, UserId, "Work");
            var service = new TagService(db);

            var result = await service.UpdateAsync(UserId, tagId, new TagUpdateRequest { Name = "Personal" });

            Assert.False(result.Success);
            Assert.Equal(EntryError.Conflict, result.Error);
        }

        // --- Delete ---

        [Fact]
        public async Task DeleteAsync_Owned_Removes()
        {
            using var db = CreateContext();
            var tagId = await SeedTagAsync(db, UserId, "Work");
            var service = new TagService(db);

            var result = await service.DeleteAsync(UserId, tagId);

            Assert.True(result.Success);
            Assert.Equal(0, await db.Tags.CountAsync()); // filtered out by soft delete
        }

        [Fact]
        public async Task DeleteAsync_NotOwned_ReturnsNotFound()
        {
            using var db = CreateContext();
            var tagId = await SeedTagAsync(db, OtherUserId, "Work");
            var service = new TagService(db);

            var result = await service.DeleteAsync(UserId, tagId);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
            Assert.Equal(1, await db.Tags.CountAsync());
        }
    }
}
