using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Controllers;
using TheShed.Server.Services;
using TheShed.Shared.Helpers;
using TheShed.Shared.Models.DTOs.Health;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class HealthControllerTests
    {
        private const int UserId = 7;

        private static HealthController CreateController(IPasswordHealthService service)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "test");
            return new HealthController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        [Fact]
        public async Task GetPasswordHealth_Returns200AndForwardsUserId()
        {
            var fake = new FakeHealthService { Report = new List<PasswordHealthItem>() };
            var controller = CreateController(fake);

            var result = await controller.GetPasswordHealth(CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(UserId, fake.LastUserId);
        }

        [Fact]
        public async Task GetPasswordHealth_ReturnsServiceReport()
        {
            var report = new List<PasswordHealthItem>
            {
                new() { EntryId = 1, VaultId = 1, EntryName = "Gmail", Strength = PasswordStrength.Weak, IsReused = true }
            };
            var fake = new FakeHealthService { Report = report };
            var controller = CreateController(fake);

            var result = await controller.GetPasswordHealth(CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(report, ok.Value);
        }

        private sealed class FakeHealthService : IPasswordHealthService
        {
            public int LastUserId { get; private set; }
            public IReadOnlyList<PasswordHealthItem> Report { get; set; } = default!;

            public Task<IReadOnlyList<PasswordHealthItem>> GetReportAsync(int userId, CancellationToken ct = default)
            {
                LastUserId = userId;
                return Task.FromResult(Report);
            }
        }
    }
}
