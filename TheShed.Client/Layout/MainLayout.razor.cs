using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TheShed.Client.Layout;

public partial class MainLayout
{
    private ErrorBoundary? _errorBoundary;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Body changes on every navigation, so this clears a previous page's error instead of
    // leaving the boundary stuck showing it forever.
    protected override void OnParametersSet() => _errorBoundary?.Recover();

    private void Reload() => Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
}
