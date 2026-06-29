using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Vaults;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class VaultsControllerTests
    {
        private const int UserId = 7;

        // Builds a controller whose User carries the given id in the NameIdentifier claim.
        private static VaultsController CreateController(IVaultService service)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "test");
            return new VaultsController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static VaultResponse SampleResponse() => new()
        {
            Id = 1,
            Name = "Personal",
            IsOwner = true,
            CanWrite = true
        };

        [Fact]
        public async Task List_Returns200AndForwardsUserId()
        {
            var fake = new FakeVaultService { ListResult = new List<VaultListItem>() };
            var controller = CreateController(fake);

            var result = await controller.List(CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(UserId, fake.LastUserId); // controller pulled the id from the JWT claim
        }

        [Fact]
        public async Task Get_NotFound_Returns404()
        {
            var fake = new FakeVaultService { GetResult = EntryResult<VaultResponse>.Fail(EntryError.NotFound) };
            var controller = CreateController(fake);

            var result = await controller.Get(id: 1, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Returns201()
        {
            var fake = new FakeVaultService { CreateResult = SampleResponse() };
            var controller = CreateController(fake);

            var result = await controller.Create(new VaultCreateRequest { Name = "Personal" }, CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsType<VaultResponse>(created.Value);
        }

        [Fact]
        public async Task Update_Forbidden_Returns403()
        {
            var fake = new FakeVaultService { UpdateResult = EntryResult<VaultResponse>.Fail(EntryError.Forbidden) };
            var controller = CreateController(fake);

            var result = await controller.Update(id: 1, new VaultUpdateRequest { Name = "x" }, CancellationToken.None);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Delete_Success_Returns204()
        {
            var fake = new FakeVaultService { DeleteResult = EntryResult<bool>.Ok(true) };
            var controller = CreateController(fake);

            var result = await controller.Delete(id: 1, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task AddMember_UserNotFound_Returns404()
        {
            var fake = new FakeVaultService { AddMemberResult = EntryResult<VaultMemberItem>.Fail(EntryError.UserNotFound) };
            var controller = CreateController(fake);

            var result = await controller.AddMember(id: 1, new VaultMemberAddRequest { Email = "x@y.com" }, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddMember_AlreadyMember_Returns409()
        {
            var fake = new FakeVaultService { AddMemberResult = EntryResult<VaultMemberItem>.Fail(EntryError.AlreadyMember) };
            var controller = CreateController(fake);

            var result = await controller.AddMember(id: 1, new VaultMemberAddRequest { Email = "x@y.com" }, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task RemoveMember_Success_Returns204()
        {
            var fake = new FakeVaultService { RemoveMemberResult = EntryResult<bool>.Ok(true) };
            var controller = CreateController(fake);

            var result = await controller.RemoveMember(id: 1, memberUserId: 2, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        // Fake service: returns preconfigured results and records the user id it received.
        private sealed class FakeVaultService : IVaultService
        {
            public int LastUserId { get; private set; }
            public IReadOnlyList<VaultListItem> ListResult { get; set; } = default!;
            public EntryResult<VaultResponse> GetResult { get; set; } = default!;
            public VaultResponse CreateResult { get; set; } = default!;
            public EntryResult<VaultResponse> UpdateResult { get; set; } = default!;
            public EntryResult<bool> DeleteResult { get; set; } = default!;
            public EntryResult<IReadOnlyList<VaultMemberItem>> ListMembersResult { get; set; } = default!;
            public EntryResult<VaultMemberItem> AddMemberResult { get; set; } = default!;
            public EntryResult<VaultMemberItem> UpdateMemberRoleResult { get; set; } = default!;
            public EntryResult<bool> RemoveMemberResult { get; set; } = default!;

            public Task<IReadOnlyList<VaultListItem>> ListAsync(int userId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(ListResult);
            }

            public Task<EntryResult<VaultResponse>> GetAsync(int userId, int vaultId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(GetResult);
            }

            public Task<VaultResponse> CreateAsync(int userId, VaultCreateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(CreateResult);
            }

            public Task<EntryResult<VaultResponse>> UpdateAsync(int userId, int vaultId, VaultUpdateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(UpdateResult);
            }

            public Task<EntryResult<bool>> DeleteAsync(int userId, int vaultId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(DeleteResult);
            }

            public Task<EntryResult<IReadOnlyList<VaultMemberItem>>> ListMembersAsync(int userId, int vaultId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(ListMembersResult);
            }

            public Task<EntryResult<VaultMemberItem>> AddMemberAsync(int userId, int vaultId, VaultMemberAddRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(AddMemberResult);
            }

            public Task<EntryResult<VaultMemberItem>> UpdateMemberRoleAsync(int userId, int vaultId, int memberUserId, VaultMemberRoleUpdateRequest request, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(UpdateMemberRoleResult);
            }

            public Task<EntryResult<bool>> RemoveMemberAsync(int userId, int vaultId, int memberUserId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(RemoveMemberResult);
            }
        }
    }
}
