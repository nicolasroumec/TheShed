using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Attachments;
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
    [Inject] private AttachmentClient AttachmentApi { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private static readonly TimeSpan CopiedLabelDuration = TimeSpan.FromSeconds(2);

    private string? _revealedPassword;
    private bool _copied;
    private DateTime? _lastPasswordChangedAt;

    private bool _historyOpen;
    private IReadOnlyList<EntryHistoryItem>? _history;
    private readonly Dictionary<int, string> _revealedHistory = new(); // historyId -> decrypted old password

    private bool _attachmentsOpen;
    private IReadOnlyList<AttachmentResponse>? _attachments;
    private bool _attachmentBusy;
    private string? _attachmentError;

    protected override void OnParametersSet()
    {
        // The password may have changed under us (an edit in the parent's form). Stale reveal/
        // history caches would otherwise keep showing the old password.
        if (_lastPasswordChangedAt is not null && _lastPasswordChangedAt != Entry.PasswordChangedAt)
        {
            _revealedPassword = null;
            _historyOpen = false;
            _history = null;
            _revealedHistory.Clear();
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

    private async Task ToggleHistoryAsync()
    {
        _historyOpen = !_historyOpen;
        if (_historyOpen && _history is null)
        {
            _history = await EntryApi.GetHistoryAsync(Entry.Id);
        }
    }

    private async Task ToggleHistoryRevealAsync(int historyId)
    {
        if (_revealedHistory.Remove(historyId))
        {
            return; // was shown, now hidden
        }

        var detail = await EntryApi.GetHistoryEntryAsync(Entry.Id, historyId);
        if (detail is not null)
        {
            _revealedHistory[historyId] = detail.Password;
        }
    }

    // --- Attachments ---

    private async Task ToggleAttachmentsAsync()
    {
        _attachmentsOpen = !_attachmentsOpen;
        if (_attachmentsOpen && _attachments is null)
        {
            _attachments = await AttachmentApi.ListAsync(Entry.Id);
        }
    }

    private static string FileSizeText(long bytes) => bytes < 1024 * 1024
        ? $"{bytes / 1024.0:0.#} KB"
        : $"{bytes / 1024.0 / 1024.0:0.#} MB";

    private async Task UploadAttachmentAsync(InputFileChangeEventArgs e)
    {
        _attachmentError = null;
        _attachmentBusy = true;
        try
        {
            var response = await AttachmentApi.UploadAsync(Entry.Id, e.File);
            if (!response.IsSuccessStatusCode)
            {
                _attachmentError = "Could not upload the file. Check the size (max 5 MB) and type (.pdf, .jpg, .jpeg, .png, .txt).";
                return;
            }
            _attachments = await AttachmentApi.ListAsync(Entry.Id);
        }
        catch (Exception)
        {
            _attachmentError = "Could not upload the file. Please try again.";
        }
        finally
        {
            _attachmentBusy = false;
        }
    }

    private async Task DownloadAttachmentAsync(AttachmentResponse file)
    {
        var (fileName, content) = await AttachmentApi.DownloadAsync(Entry.Id, file.Id);
        await JS.InvokeVoidAsync("downloadFile", fileName, content);
    }

    private async Task DeleteAttachmentAsync(int attachmentId)
    {
        if (!await JS.InvokeAsync<bool>("confirm", "Delete this file? This cannot be undone."))
        {
            return;
        }

        _attachmentError = null;
        try
        {
            await AttachmentApi.DeleteAsync(Entry.Id, attachmentId);
            _attachments = await AttachmentApi.ListAsync(Entry.Id);
        }
        catch (Exception)
        {
            _attachmentError = "Could not delete the file. Please try again.";
        }
    }

    private async Task DeleteAsync()
    {
        if (!await JS.InvokeAsync<bool>("confirm", "Delete this entry? This cannot be undone."))
        {
            return;
        }

        await OnDeleteRequested.InvokeAsync(Entry.Id);
    }
}
