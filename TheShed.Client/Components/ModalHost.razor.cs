using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TheShed.Client.Services;

namespace TheShed.Client.Components;

public partial class ModalHost : IDisposable
{
    [Inject] private ModalService Modal { get; set; } = default!;

    private ModalRequest? _request;
    private ElementReference _passwordInput;
    private string _password = string.Empty;
    private bool _focusPassword;
    private bool _busy;

    protected override void OnInitialized()
    {
        Modal.OnShow += HandleShow;
        Modal.OnClose += HandleClose;
    }

    private void HandleShow(ModalRequest request)
    {
        _request = request;
        _password = string.Empty;
        _busy = false;
        _focusPassword = request.RequiresPassword;
        StateHasChanged(); // raised from ModalService's event, not our own render pipeline
    }

    /// <summary>The caller accepted the answer it was handed, so the busy prompt can go.</summary>
    private void HandleClose()
    {
        Close();
        StateHasChanged();
    }

    // The field does not exist until the modal renders, so autofocus has to wait for it.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPassword && _request is not null)
        {
            _focusPassword = false;
            await _passwordInput.FocusAsync();
        }
    }

    private Task HandlePasswordKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            Confirm();
        }
        return Task.CompletedTask;
    }

    private void Confirm()
    {
        if (_request?.RequiresPassword == true)
        {
            // Stay open and busy: checking this answer costs a ~1s key derivation, and the caller
            // decides what happens next — prompt again (wrong) or Close() (accepted).
            _busy = true;
            Modal.Resolve(_password);
            return;
        }

        // A plain confirm has no text to hand back, so it resolves with an empty string: what
        // separates yes from cancel is null, not the content.
        Close();
        Modal.Resolve(string.Empty);
    }

    private void Cancel()
    {
        Close();
        Modal.Resolve(null);
    }

    private void Close()
    {
        _request = null;
        _password = string.Empty;
        _busy = false;
    }

    public void Dispose()
    {
        Modal.OnShow -= HandleShow;
        Modal.OnClose -= HandleClose;
    }
}
