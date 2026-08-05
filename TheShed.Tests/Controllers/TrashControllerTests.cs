using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Trash;
using TheShed.Shared.Models.Enums;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class TrashControllerTests
    {
        private const int UserId = 7;

        // Builds a controller whose User carries the given id in the NameIdentifier claim.
        private static TrashController CreateController(ITrashService service)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "test");
            return new TrashController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        [Fact]
        public async Task List_Success_Returns200AndForwardsUserId()
        {
            var fake = new FakeTrashService { ListResult = new List<TrashItem>() };
            var controller = CreateController(fake);

            var result = await controller.List(CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(UserId, fake.LastUserId); // controller pulled the id from the JWT claim
        }

        [Fact]
        public async Task Restore_Success_Returns204()
        {
            var fake = new FakeTrashService { RestoreResult = EntryResult<bool>.Ok(true) };
            var controller = CreateController(fake);

            var result = await controller.Restore(TrashItemType.Entry, id: 1, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Restore_NotFound_Returns404()
        {
            var fake = new FakeTrashService { RestoreResult = EntryResult<bool>.Fail(EntryError.NotFound) };
            var controller = CreateController(fake);

            var result = await controller.Restore(TrashItemType.Vault, id: 1, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Restore_Forbidden_Returns403()
        {
            var fake = new FakeTrashService { RestoreResult = EntryResult<bool>.Fail(EntryError.Forbidden) };
            var controller = CreateController(fake);

            var result = await controller.Restore(TrashItemType.Note, id: 1, CancellationToken.None);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Purge_Success_Returns204()
        {
            var fake = new FakeTrashService { PurgeResult = EntryResult<bool>.Ok(true) };
            var controller = CreateController(fake);

            var result = await controller.Purge(TrashItemType.Entry, id: 1, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Purge_NotFound_Returns404()
        {
            var fake = new FakeTrashService { PurgeResult = EntryResult<bool>.Fail(EntryError.NotFound) };
            var controller = CreateController(fake);

            var result = await controller.Purge(TrashItemType.Vault, id: 1, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        // Fake service: returns preconfigured results and records the user id it received.
        private sealed class FakeTrashService : ITrashService
        {
            public int LastUserId { get; private set; }
            public IReadOnlyList<TrashItem> ListResult { get; set; } = new List<TrashItem>();
            public EntryResult<bool> RestoreResult { get; set; } = default!;
            public EntryResult<bool> PurgeResult { get; set; } = default!;

            public Task<IReadOnlyList<TrashItem>> ListAsync(int userId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(ListResult);
            }

            public Task<EntryResult<bool>> RestoreAsync(int userId, TrashItemType type, int id, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(RestoreResult);
            }

            public Task<EntryResult<bool>> PurgeAsync(int userId, TrashItemType type, int id, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(PurgeResult);
            }

            public Task<int> PurgeExpiredAsync(DateTime now, CancellationToken ct = default) =>
                Task.FromResult(0);
        }
    }
}
