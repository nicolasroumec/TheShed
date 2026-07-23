using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Tags;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class TagsControllerTests
    {
        private const int UserId = 7;

        private static TagsController CreateController(ITagService service)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "test");
            return new TagsController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static TagResponse SampleTag() => new() { Id = 1, Name = "Work" };

        [Fact]
        public async Task List_Success_Returns200AndForwardsUserId()
        {
            var fake = new FakeTagService
            {
                ListResult = EntryResult<IReadOnlyList<TagResponse>>.Ok(new List<TagResponse>())
            };
            var controller = CreateController(fake);

            var result = await controller.List(CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(UserId, fake.LastUserId); // controller pulled the id from the JWT claim
        }

        [Fact]
        public async Task Create_Success_Returns201()
        {
            var fake = new FakeTagService { CreateResult = EntryResult<TagResponse>.Ok(SampleTag()) };
            var controller = CreateController(fake);

            var result = await controller.Create(new TagCreateRequest { Name = "Work" }, CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsType<TagResponse>(created.Value);
        }

        [Fact]
        public async Task Create_Conflict_Returns409()
        {
            var fake = new FakeTagService { CreateResult = EntryResult<TagResponse>.Fail(EntryError.Conflict) };
            var controller = CreateController(fake);

            var result = await controller.Create(new TagCreateRequest { Name = "Work" }, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Update_NotFound_Returns404()
        {
            var fake = new FakeTagService { UpdateResult = EntryResult<TagResponse>.Fail(EntryError.NotFound) };
            var controller = CreateController(fake);

            var result = await controller.Update(id: 1, new TagUpdateRequest { Name = "Job" }, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Success_Returns204()
        {
            var fake = new FakeTagService { DeleteResult = EntryResult<bool>.Ok(true) };
            var controller = CreateController(fake);

            var result = await controller.Delete(id: 1, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        // Fake service: returns preconfigured results and records the user id it received.
        private sealed class FakeTagService : ITagService
        {
            public int LastUserId { get; private set; }
            public EntryResult<IReadOnlyList<TagResponse>> ListResult { get; set; } = default!;
            public EntryResult<TagResponse> CreateResult { get; set; } = default!;
            public EntryResult<TagResponse> UpdateResult { get; set; } = default!;
            public EntryResult<bool> DeleteResult { get; set; } = default!;

            public Task<EntryResult<IReadOnlyList<TagResponse>>> ListAsync(int userId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(ListResult);
            }

            public Task<EntryResult<TagResponse>> CreateAsync(int userId, TagCreateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(CreateResult);
            }

            public Task<EntryResult<TagResponse>> UpdateAsync(int userId, int tagId, TagUpdateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(UpdateResult);
            }

            public Task<EntryResult<bool>> DeleteAsync(int userId, int tagId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(DeleteResult);
            }
        }
    }
}
