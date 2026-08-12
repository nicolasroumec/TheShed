using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;

namespace TheShed.Client.Components;

public partial class ModalHost : IDisposable
{
    [Inject] private ModalService Modal { get; set; } = default!;

    private ModalRequest? _request;

    protected override void OnInitialized()
    {
        Modal.OnShow += HandleShow;
    }

    private void HandleShow(ModalRequest request)
    {
        _request = request;
        StateHasChanged(); // raised from ModalService's event, not our own render pipeline
    }

    private void Confirm()
    {
        _request = null;
        Modal.Resolve(true);
    }

    private void Cancel()
    {
        _request = null;
        Modal.Resolve(false);
    }

    public void Dispose() => Modal.OnShow -= HandleShow;
}
