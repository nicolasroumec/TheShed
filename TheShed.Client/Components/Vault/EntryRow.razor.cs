using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Entries;

namespace TheShed.Client.Components.Vault;

public partial class EntryRow
{
    [Parameter, EditorRequired] public EntryListItem Entry { get; set; } = default!;
    [Parameter] public bool CanWrite { get; set; }
    [Parameter] public EventCallback<EntryListItem> OnFavoriteToggled { get; set; }
    [Parameter] public EventCallback<int> OnDeleteRequested { get; set; }
    [Parameter] public EventCallback<int> OnEditRequested { get; set; }

    [Inject] private EntryClient EntryApi { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    private static readonly TimeSpan CopiedLabelDuration = TimeSpan.FromSeconds(2);

    private string? _revealedPassword;
    private bool _copied;
    private DateTime? _lastPasswordChangedAt;

    private bool _historyOpen;
    private bool _attachmentsOpen;

    protected override void OnParametersSet()
    {
        // The password may have changed under us (an edit in the parent's form). Close the
        // panels so the user has to reopen (and refetch) instead of seeing stale data.
        if (_lastPasswordChangedAt is not null && _lastPasswordChangedAt != Entry.PasswordChangedAt)
        {
            _revealedPassword = null;
            _historyOpen = false;
        }
        _lastPasswordChangedAt = Entry.PasswordChangedAt;
    }

    private static string DaysAgoText(DateTime changedAt)
    {
        var days = (int)(DateTime.UtcNow - changedAt).TotalDays;
        return days switch
        {
            0 => "today",
            1 => "1 day ago",
            _ => $"{days} days ago"
        };
    }

    private async Task ToggleRevealAsync()
    {
        if (_revealedPassword is not null)
        {
            _revealedPassword = null;
            return;
        }

        var entry = await EntryApi.GetAsync(Entry.Id);
        if (entry is not null)
        {
            _revealedPassword = entry.Password;
        }
    }

    private async Task CopyPasswordAsync()
    {
        var entry = await EntryApi.GetAsync(Entry.Id);
        if (entry is null)
        {
            return;
        }

        await JS.InvokeVoidAsync("copyToClipboard", entry.Password);

        // Flash "Copied!" and put the button back. StateHasChanged is needed here because the
        // label has to update before the delay, not after the handler finally returns.
        _copied = true;
        StateHasChanged();
        await Task.Delay(CopiedLabelDuration);
        _copied = false;
    }

    private async Task DeleteAsync()
    {
        if (!await Modal.ConfirmAsync("Delete this entry? This cannot be undone.", title: "Delete entry", confirmText: "Delete"))
        {
            return;
        }

        await OnDeleteRequested.InvokeAsync(Entry.Id);
    }
}
