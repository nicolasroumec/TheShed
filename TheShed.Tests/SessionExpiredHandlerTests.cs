using System.Net;
using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using Xunit;

namespace TheShed.Tests;

file class RecordingNavigationManager : NavigationManager
{
    public string? NavigatedTo { get; private set; }

    public RecordingNavigationManager() => Initialize("https://localhost/", "https://localhost/vaults");

    protected override void NavigateToCore(string uri, NavigationOptions options) => NavigatedTo = uri;
}

file class StatusHandler(HttpStatusCode status) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
        Task.FromResult(new HttpResponseMessage(status));
}

public class SessionExpiredHandlerTests
{
    private static async Task<string?> SendAsync(string url, HttpStatusCode status)
    {
        var nav = new RecordingNavigationManager();
        var client = new HttpClient(new SessionExpiredHandler(nav) { InnerHandler = new StatusHandler(status) })
        {
            BaseAddress = new Uri("https://localhost/")
        };
        await client.GetAsync(url);
        return nav.NavigatedTo;
    }

    [Fact]
    public async Task Unauthorized_OnApiCall_RedirectsToLogin() =>
        Assert.Equal("login", await SendAsync("api/vaults", HttpStatusCode.Unauthorized));

    [Theory]
    [InlineData("api/auth/login")]
    [InlineData("api/auth/me")]
    public async Task Unauthorized_OnExemptEndpoint_DoesNotRedirect(string url) =>
        Assert.Null(await SendAsync(url, HttpStatusCode.Unauthorized));

    [Fact]
    public async Task OtherStatus_DoesNotRedirect() =>
        Assert.Null(await SendAsync("api/vaults", HttpStatusCode.Forbidden));
}
