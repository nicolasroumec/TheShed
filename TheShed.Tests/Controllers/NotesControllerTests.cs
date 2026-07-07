using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Notes;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class NotesControllerTests
    {
        private const int UserId = 7;

        // Builds a controller whose User carries the given id in the NameIdentifier claim.
        private static NotesController CreateController(ISecureNoteService service)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "test");
            return new NotesController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static NoteResponse SampleResponse() => new()
        {
            Id = 1,
            VaultId = 1,
            Title = "Recovery codes",
            Content = "top-secret-content"
        };

        [Fact]
        public async Task List_Success_Returns200AndForwardsUserId()
        {
            var fake = new FakeNoteService
            {
                ListResult = EntryResult<IReadOnlyList<NoteListItem>>.Ok(new List<NoteListItem>())
            };
            var controller = CreateController(fake);

            var result = await controller.List(vaultId: 1, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(UserId, fake.LastUserId); // controller pulled the id from the JWT claim
        }

        [Fact]
        public async Task Get_NotFound_Returns404()
        {
            var fake = new FakeNoteService { GetResult = EntryResult<NoteResponse>.Fail(EntryError.NotFound) };
            var controller = CreateController(fake);

            var result = await controller.Get(id: 1, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Success_Returns201()
        {
            var fake = new FakeNoteService { CreateResult = EntryResult<NoteResponse>.Ok(SampleResponse()) };
            var controller = CreateController(fake);

            var result = await controller.Create(new NoteCreateRequest(), CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsType<NoteResponse>(created.Value);
        }

        [Fact]
        public async Task Create_Forbidden_Returns403()
        {
            var fake = new FakeNoteService { CreateResult = EntryResult<NoteResponse>.Fail(EntryError.Forbidden) };
            var controller = CreateController(fake);

            var result = await controller.Create(new NoteCreateRequest(), CancellationToken.None);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Delete_Success_Returns204()
        {
            var fake = new FakeNoteService { DeleteResult = EntryResult<bool>.Ok(true) };
            var controller = CreateController(fake);

            var result = await controller.Delete(id: 1, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        // Fake service: returns preconfigured results and records the user id it received.
        private sealed class FakeNoteService : ISecureNoteService
        {
            public int LastUserId { get; private set; }
            public EntryResult<IReadOnlyList<NoteListItem>> ListResult { get; set; } = default!;
            public EntryResult<NoteResponse> GetResult { get; set; } = default!;
            public EntryResult<NoteResponse> CreateResult { get; set; } = default!;
            public EntryResult<NoteResponse> UpdateResult { get; set; } = default!;
            public EntryResult<bool> DeleteResult { get; set; } = default!;

            public Task<EntryResult<IReadOnlyList<NoteListItem>>> ListAsync(int userId, int vaultId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(ListResult);
            }

            public Task<EntryResult<NoteResponse>> GetAsync(int userId, int noteId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(GetResult);
            }

            public Task<EntryResult<NoteResponse>> CreateAsync(int userId, NoteCreateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(CreateResult);
            }

            public Task<EntryResult<NoteResponse>> UpdateAsync(int userId, int noteId, NoteUpdateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(UpdateResult);
            }

            public Task<EntryResult<bool>> DeleteAsync(int userId, int noteId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(DeleteResult);
            }
        }
    }
}
