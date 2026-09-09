using Microsoft.AspNetCore.Components;

namespace TheShed.Client.Auth;

public partial class RedirectToLogin
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Sends unauthenticated users to the login page, preserving the page they
    // tried to reach so we can return there after a successful login.
    protected override void OnInitialized()
    {
        var returnUrl = Uri.EscapeDataString(
            Navigation.ToBaseRelativePath(Navigation.Uri));
        Navigation.NavigateTo($"login?returnUrl={returnUrl}", forceLoad: false);
    }
}
