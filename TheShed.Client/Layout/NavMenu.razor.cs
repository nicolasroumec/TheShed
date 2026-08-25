using System.Reflection;
using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;

namespace TheShed.Client.Layout;

public partial class NavMenu
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // MinVer pins AssemblyVersion (GetName().Version) to a fixed low value to avoid binding-redirect
    // churn on every build — the real semver MinVer computes from git tags lives in
    // AssemblyInformationalVersion instead. That also carries a "+<sha>" build-metadata suffix,
    // trimmed here since it's noise for a navbar label.
    private static readonly string AppVersion =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0]
        ?? "dev";

    private bool _collapseNavMenu = true;

    private string? NavMenuCssClass => _collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        _collapseNavMenu = !_collapseNavMenu;
    }

    private async Task Logout()
    {
        await AuthService.LogoutAsync();
        Navigation.NavigateTo("login");
    }
}
