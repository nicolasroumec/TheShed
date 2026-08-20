using System.Reflection;
using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;

namespace TheShed.Client.Layout;

public partial class NavMenu
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private static readonly string AppVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "dev";

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
