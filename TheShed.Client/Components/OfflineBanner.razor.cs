using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TheShed.Client.Components;

public partial class OfflineBanner : IAsyncDisposable
{
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private bool _isOnline = true;
    private DotNetObjectReference<OfflineBanner>? _selfRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _selfRef = DotNetObjectReference.Create(this);
        await Js.InvokeVoidAsync("startConnectivityWatch", _selfRef);
    }

    [JSInvokable]
    public Task OnConnectivityChange(bool isOnline)
    {
        _isOnline = isOnline;
        return InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_selfRef is null)
        {
            return;
        }

        try
        {
            await Js.InvokeVoidAsync("stopConnectivityWatch");
        }
        catch (JSDisconnectedException)
        {
            // The page is already gone; there is nothing left to unhook.
        }

        _selfRef.Dispose();
    }
}
