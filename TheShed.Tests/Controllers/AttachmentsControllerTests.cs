using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Attachments;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class AttachmentsControllerTests
    {
        private const int UserId = 7;
        private const int EntryId = 1;

        // Builds a controller whose User carries the given id in the NameIdentifier claim.
        private static AttachmentsController CreateController(IAttachmentService service)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "test");
            return new AttachmentsController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static IFormFile SampleFile(string name = "card.png") =>
            new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("content")), 0, 7, "file", name);

        [Fact]
        public async Task List_Success_Returns200AndForwardsUserId()
        {
            var fake = new FakeAttachmentService
            {
                ListResult = EntryResult<IReadOnlyList<AttachmentResponse>>.Ok(new List<AttachmentResponse>())
            };
            var controller = CreateController(fake);

            var result = await controller.List(EntryId, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(UserId, fake.LastUserId); // controller pulled the id from the JWT claim
        }

        [Fact]
        public async Task Upload_Success_Returns201()
        {
            var fake = new FakeAttachmentService
            {
                UploadResult = EntryResult<AttachmentResponse>.Ok(new AttachmentResponse { Id = 1, FileName = "card.png" })
            };
            var controller = CreateController(fake);

            var result = await controller.Upload(EntryId, SampleFile(), CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsType<AttachmentResponse>(created.Value);
        }

        [Fact]
        public async Task Upload_InvalidFile_Returns400()
        {
            var fake = new FakeAttachmentService { UploadResult = EntryResult<AttachmentResponse>.Fail(EntryError.InvalidFile) };
            var controller = CreateController(fake);

            var result = await controller.Upload(EntryId, SampleFile(), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Download_Success_ReturnsFileContent()
        {
            var fake = new FakeAttachmentService
            {
                DownloadResult = EntryResult<(string, byte[])>.Ok(("card.png", [1, 2, 3]))
            };
            var controller = CreateController(fake);

            var result = await controller.Download(EntryId, 1, CancellationToken.None);

            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("card.png", file.FileDownloadName);
            Assert.Equal(new byte[] { 1, 2, 3 }, file.FileContents);
        }

        [Fact]
        public async Task Download_NotFound_Returns404()
        {
            var fake = new FakeAttachmentService { DownloadResult = EntryResult<(string, byte[])>.Fail(EntryError.NotFound) };
            var controller = CreateController(fake);

            var result = await controller.Download(EntryId, 1, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Success_Returns204()
        {
            var fake = new FakeAttachmentService { DeleteResult = EntryResult<bool>.Ok(true) };
            var controller = CreateController(fake);

            var result = await controller.Delete(EntryId, 1, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_Forbidden_Returns403()
        {
            var fake = new FakeAttachmentService { DeleteResult = EntryResult<bool>.Fail(EntryError.Forbidden) };
            var controller = CreateController(fake);

            var result = await controller.Delete(EntryId, 1, CancellationToken.None);

            Assert.IsType<ForbidResult>(result);
        }

        // Fake service: returns preconfigured results and records the user id it received.
        private sealed class FakeAttachmentService : IAttachmentService
        {
            public int LastUserId { get; private set; }
            public EntryResult<IReadOnlyList<AttachmentResponse>> ListResult { get; set; } = default!;
            public EntryResult<AttachmentResponse> UploadResult { get; set; } = default!;
            public EntryResult<(string FileName, byte[] Content)> DownloadResult { get; set; } = default!;
            public EntryResult<bool> DeleteResult { get; set; } = default!;

            public Task<EntryResult<IReadOnlyList<AttachmentResponse>>> ListAsync(int userId, int entryId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(ListResult);
            }

            public Task<EntryResult<AttachmentResponse>> UploadAsync(int userId, int entryId, string fileName, byte[] content, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(UploadResult);
            }

            public Task<EntryResult<(string FileName, byte[] Content)>> DownloadAsync(int userId, int entryId, int attachmentId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(DownloadResult);
            }

            public Task<EntryResult<bool>> DeleteAsync(int userId, int entryId, int attachmentId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(DeleteResult);
            }
        }
    }
}
