using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Entries;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class EntriesControllerTests
    {
        private const int UserId = 7;

        // Builds a controller whose User carries the given id in the NameIdentifier claim.
        private static EntriesController CreateController(IPasswordEntryService service)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "test");
            return new EntriesController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static EntryResponse SampleResponse() => new()
        {
            Id = 1,
            VaultId = 1,
            Name = "Gmail",
            Username = "ana@gmail.com",
            Password = "secret"
        };

        [Fact]
        public async Task List_Success_Returns200AndForwardsUserId()
        {
            var fake = new FakeEntryService
            {
                ListResult = EntryResult<IReadOnlyList<EntryListItem>>.Ok(new List<EntryListItem>())
            };
            var controller = CreateController(fake);

            var result = await controller.List(vaultId: 1, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(UserId, fake.LastUserId); // controller pulled the id from the JWT claim
        }

        [Fact]
        public async Task Get_NotFound_Returns404()
        {
            var fake = new FakeEntryService { GetResult = EntryResult<EntryResponse>.Fail(EntryError.NotFound) };
            var controller = CreateController(fake);

            var result = await controller.Get(id: 1, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Success_Returns201()
        {
            var fake = new FakeEntryService { CreateResult = EntryResult<EntryResponse>.Ok(SampleResponse()) };
            var controller = CreateController(fake);

            var result = await controller.Create(new EntryCreateRequest(), CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsType<EntryResponse>(created.Value);
        }

        [Fact]
        public async Task Create_Forbidden_Returns403()
        {
            var fake = new FakeEntryService { CreateResult = EntryResult<EntryResponse>.Fail(EntryError.Forbidden) };
            var controller = CreateController(fake);

            var result = await controller.Create(new EntryCreateRequest(), CancellationToken.None);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Delete_Success_Returns204()
        {
            var fake = new FakeEntryService { DeleteResult = EntryResult<bool>.Ok(true) };
            var controller = CreateController(fake);

            var result = await controller.Delete(id: 1, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        // Fake service: returns preconfigured results and records the user id it received.
        private sealed class FakeEntryService : IPasswordEntryService
        {
            public int LastUserId { get; private set; }
            public EntryResult<IReadOnlyList<EntryListItem>> ListResult { get; set; } = default!;
            public EntryResult<EntryResponse> GetResult { get; set; } = default!;
            public EntryResult<EntryResponse> CreateResult { get; set; } = default!;
            public EntryResult<EntryResponse> UpdateResult { get; set; } = default!;
            public EntryResult<bool> DeleteResult { get; set; } = default!;

            public Task<EntryResult<IReadOnlyList<EntryListItem>>> ListAsync(int userId, int vaultId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(ListResult);
            }

            public Task<EntryResult<EntryResponse>> GetAsync(int userId, int entryId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(GetResult);
            }

            public Task<EntryResult<EntryResponse>> CreateAsync(int userId, EntryCreateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(CreateResult);
            }

            public Task<EntryResult<EntryResponse>> UpdateAsync(int userId, int entryId, EntryUpdateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(UpdateResult);
            }

            public Task<EntryResult<bool>> DeleteAsync(int userId, int entryId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(DeleteResult);
            }
        }
    }
}
