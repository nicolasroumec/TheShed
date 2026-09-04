using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TheShed.Server.Controllers;
using TheShed.Shared.Models.DTOs.Auth;
using Xunit;

namespace TheShed.Tests.Controllers
{
    public class AntiforgeryControllerTests
    {
        // Real IAntiforgery (not a fake) — the thing worth checking here is that
        // GetAndStoreTokens actually runs and both issues a non-empty token and sets the
        // pairing cookie on the response, which a hand-rolled fake wouldn't prove.
        private static AntiforgeryController CreateController()
        {
            var services = new ServiceCollection();
            services.AddAntiforgery();
            services.AddLogging();
            var provider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = provider };
            return new AntiforgeryController(provider.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>())
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        [Fact]
        public void GetToken_DevuelveTokenNoVacio()
        {
            var controller = CreateController();

            var result = controller.GetToken();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AntiforgeryTokenResponse>(ok.Value);
            Assert.False(string.IsNullOrWhiteSpace(response.Token));
        }

        [Fact]
        public void GetToken_SeteaCookieDeAntiforgery()
        {
            var controller = CreateController();

            controller.GetToken();

            var setCookie = controller.HttpContext.Response.Headers.SetCookie.ToString();
            Assert.Contains("Antiforgery", setCookie);
        }
    }
}
