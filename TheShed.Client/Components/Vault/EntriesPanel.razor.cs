using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TheShed.Client.Services;
using TheShed.Shared.Helpers;
using TheShed.Shared.Models.DTOs.Attachments;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Models.DTOs.Tags;

namespace TheShed.Client.Components.Vault;

public partial class EntriesPanel : IDisposable
{
    [Parameter] public int VaultId { get; set; }
    [Parameter] public bool CanWrite { get; set; }

    [Inject] private EntryClient EntryApi { get; set; } = default!;
    [Inject] private TagClient TagApi { get; set; } = default!;
    [Inject] private AttachmentClient AttachmentApi { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private IReadOnlyList<EntryListItem>? _entries;
    private readonly Dictionary<int, string> _revealed = new();
    private int? _copiedId;         // drives the transient "Copied!" label; the password itself is never rendered
    private string? _listError;     // failures of list-level actions (favorite, delete)
    private CancellationTokenSource? _searchCts;
    private readonly HashSet<int> _historyOpenIds = new();                                   // entry ids with the history panel open
    private readonly Dictionary<int, IReadOnlyList<EntryHistoryItem>> _history = new();       // entryId -> past passwords (metadata)
    private readonly Dictionary<int, string> _revealedHistory = new();                        // historyId -> decrypted old password

    private readonly HashSet<int> _attachmentsOpenIds = new();                                // entry ids with the files panel open
    private readonly Dictionary<int, IReadOnlyList<AttachmentResponse>> _attachments = new();  // entryId -> attachments
    private int? _attachmentBusyId;     // entry id currently uploading, for the "Uploading…" label
    private string? _attachmentError;
    private int? _attachmentErrorEntryId;

    private EntryCreateRequest? _form;   // non-null while the create/edit form is open
    private int? _editingId;             // null = creating, otherwise the entry being edited
    private bool _busy;
    private string? _error;

    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan CopiedLabelDuration = TimeSpan.FromSeconds(2);

    private bool _showPassword;        // toggles the form password field between text/password
    private int _genLength = 20;       // password generator options
    private bool _genSymbols = true;

    private IReadOnlyList<TagResponse> _tags = [];      // the caller's tags (per-user)
    private int? _activeTagId;                          // null = no tag filter
    private string _search = string.Empty;               // empty = no search filter
    private readonly HashSet<int> _formTagIds = new();  // tags on the entry being edited
    private string _newTagName = string.Empty;
    private string? _tagError;

    protected override async Task OnInitializedAsync()
    {
        await LoadEntriesAsync();
        _tags = await TagApi.ListAsync();
    }

    public void StartCreate()
    {
        _editingId = null;
        _error = null;
        _form = new EntryCreateRequest { VaultId = VaultId };
    }

    private async Task ToggleRevealAsync(int entryId)
    {
        if (_revealed.Remove(entryId))
        {
            return; // was shown, now hidden
        }

        var entry = await EntryApi.GetAsync(entryId);
        if (entry is not null)
        {
            _revealed[entryId] = entry.Password;
        }
    }

    private async Task CopyPasswordAsync(int entryId)
    {
        var entry = await EntryApi.GetAsync(entryId);
        if (entry is null)
        {
            return;
        }

        await JS.InvokeVoidAsync("copyToClipboard", entry.Password);

        // Flash "Copied!" and put the button back. StateHasChanged is needed here because the
        // label has to update before the delay, not after the handler finally returns.
        _copiedId = entryId;
        StateHasChanged();
        await Task.Delay(CopiedLabelDuration);
        if (_copiedId == entryId)
        {
            _copiedId = null;
        }
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

    private async Task ToggleHistoryAsync(int entryId)
    {
        if (!_historyOpenIds.Add(entryId))
        {
            _historyOpenIds.Remove(entryId); // was open, now closed
            return;
        }

        if (!_history.ContainsKey(entryId))
        {
            _history[entryId] = await EntryApi.GetHistoryAsync(entryId);
        }
    }

    private async Task ToggleHistoryRevealAsync(int entryId, int historyId)
    {
        if (_revealedHistory.Remove(historyId))
        {
            return; // was shown, now hidden
        }

        var detail = await EntryApi.GetHistoryEntryAsync(entryId, historyId);
        if (detail is not null)
        {
            _revealedHistory[historyId] = detail.Password;
        }
    }

    // --- Attachments ---

    private async Task ToggleAttachmentsAsync(int entryId)
    {
        if (!_attachmentsOpenIds.Add(entryId))
        {
            _attachmentsOpenIds.Remove(entryId); // was open, now closed
            return;
        }

        _attachments[entryId] = await AttachmentApi.ListAsync(entryId);
    }

    private static string FileSizeText(long bytes) => bytes < 1024 * 1024
        ? $"{bytes / 1024.0:0.#} KB"
        : $"{bytes / 1024.0 / 1024.0:0.#} MB";

    private async Task UploadAttachmentAsync(int entryId, InputFileChangeEventArgs e)
    {
        _attachmentError = null;
        _attachmentBusyId = entryId;
        try
        {
            var response = await AttachmentApi.UploadAsync(entryId, e.File);
            if (!response.IsSuccessStatusCode)
            {
                _attachmentError = "Could not upload the file. Check the size (max 5 MB) and type (.pdf, .jpg, .jpeg, .png, .txt).";
                _attachmentErrorEntryId = entryId;
                return;
            }
            _attachments[entryId] = await AttachmentApi.ListAsync(entryId);
        }
        catch (Exception)
        {
            _attachmentError = "Could not upload the file. Please try again.";
            _attachmentErrorEntryId = entryId;
        }
        finally
        {
            _attachmentBusyId = null;
        }
    }

    private async Task DownloadAttachmentAsync(int entryId, AttachmentResponse file)
    {
        var (fileName, content) = await AttachmentApi.DownloadAsync(entryId, file.Id);
        await JS.InvokeVoidAsync("downloadFile", fileName, content);
    }

    private async Task DeleteAttachmentAsync(int entryId, int attachmentId)
    {
        if (!await JS.InvokeAsync<bool>("confirm", "Delete this file? This cannot be undone."))
        {
            return;
        }

        _attachmentError = null;
        try
        {
            await AttachmentApi.DeleteAsync(entryId, attachmentId);
            _attachments[entryId] = await AttachmentApi.ListAsync(entryId);
        }
        catch (Exception)
        {
            _attachmentError = "Could not delete the file. Please try again.";
            _attachmentErrorEntryId = entryId;
        }
    }

    private async Task StartEditAsync(int entryId)
    {
        var entry = await EntryApi.GetAsync(entryId);
        if (entry is null)
        {
            return;
        }

        _editingId = entryId;
        _error = null;
        _formTagIds.Clear();
        foreach (var tag in entry.Tags)
        {
            _formTagIds.Add(tag.Id);
        }
        _form = new EntryCreateRequest
        {
            VaultId = VaultId,
            Name = entry.Name,
            Username = entry.Username,
            Password = entry.Password,
            Url = entry.Url,
            Notes = entry.Notes,
            IsFavorite = entry.IsFavorite
        };
    }

    private void CancelForm()
    {
        _form = null;
        _editingId = null;
        _error = null;
        _showPassword = false;
        _formTagIds.Clear();
    }

    private async Task LoadEntriesAsync(CancellationToken cancellationToken = default)
    {
        _entries = await EntryApi.ListAsync(VaultId, _activeTagId, _search, cancellationToken);
    }

    /// <summary>Search-as-you-type: waits out the typing, then loads. Cancelling the previous
    /// token drops both the pending wait and any request already in flight, so a slow response
    /// can never land after a newer one.</summary>
    private async Task SearchInputAsync(ChangeEventArgs e)
    {
        _search = (string?)e.Value ?? string.Empty;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(SearchDebounce, token);
            await LoadEntriesAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Another keystroke arrived; that call owns the results now.
        }
    }

    private async Task ToggleFavoriteAsync(EntryListItem entry)
    {
        _listError = null;
        try
        {
            await EntryApi.SetFavoriteAsync(entry.Id, !entry.IsFavorite);
            await LoadEntriesAsync();
        }
        catch (Exception)
        {
            _listError = "Could not update the favorite. Please try again.";
        }
    }

    public void Dispose() => _searchCts?.Dispose();

    // --- Tags ---

    private async Task FilterByTagAsync(int? tagId)
    {
        _activeTagId = tagId;
        await LoadEntriesAsync();
    }

    private async Task CreateTagAsync()
    {
        var name = _newTagName.Trim();
        if (name.Length == 0)
        {
            return;
        }

        _tagError = null;
        try
        {
            await TagApi.CreateAsync(new TagCreateRequest { Name = name });
            _newTagName = string.Empty;
            _tags = await TagApi.ListAsync();
        }
        catch (Exception)
        {
            _tagError = "Could not create the tag. The name may already be in use.";
        }
    }

    private async Task TagInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await CreateTagAsync();
        }
    }

    private async Task DeleteTagAsync(TagResponse tag)
    {
        if (!await JS.InvokeAsync<bool>("confirm", $"Remove the tag \"{tag.Name}\"? It will be removed from all entries."))
        {
            return;
        }

        _tagError = null;
        try
        {
            await TagApi.DeleteAsync(tag.Id);
            if (_activeTagId == tag.Id)
            {
                _activeTagId = null; // the filtered-by tag is gone
            }
            _formTagIds.Remove(tag.Id);
            _tags = await TagApi.ListAsync();
            await LoadEntriesAsync(); // badges/filter may have changed
        }
        catch (Exception)
        {
            _tagError = "Could not remove the tag. Please try again.";
        }
    }

    private async Task ToggleEntryTagAsync(int tagId)
    {
        if (_editingId is null)
        {
            return; // assignment needs a persisted entry
        }

        if (_formTagIds.Contains(tagId))
        {
            await EntryApi.RemoveTagAsync(_editingId.Value, tagId);
            _formTagIds.Remove(tagId);
        }
        else
        {
            await EntryApi.AddTagAsync(_editingId.Value, tagId);
            _formTagIds.Add(tagId);
        }
    }

    private void GeneratePassword()
    {
        if (_form is null)
        {
            return;
        }

        var length = Math.Clamp(_genLength, 4, 128); // guard against out-of-range typed values
        _form.Password = PasswordGenerator.Generate(length, _genSymbols);
        _showPassword = true; // reveal so the user can see what was generated
    }

    private async Task SubmitAsync()
    {
        if (_form is null)
        {
            return;
        }

        _busy = true;
        _error = null;
        try
        {
            if (_editingId is null)
            {
                await EntryApi.CreateAsync(_form);
            }
            else
            {
                await EntryApi.UpdateAsync(_editingId.Value, new EntryUpdateRequest
                {
                    Name = _form.Name,
                    Username = _form.Username,
                    Password = _form.Password,
                    Url = _form.Url,
                    Notes = _form.Notes,
                    IsFavorite = _form.IsFavorite
                });
                _revealed.Remove(_editingId.Value); // stale password if it was shown
                _history.Remove(_editingId.Value);  // stale: the update may have added a new version
                _historyOpenIds.Remove(_editingId.Value);
            }

            CancelForm();
            await LoadEntriesAsync();
        }
        catch (Exception)
        {
            _error = "Could not save the entry. Please try again.";
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DeleteAsync(int entryId)
    {
        if (!await JS.InvokeAsync<bool>("confirm", "Delete this entry? This cannot be undone."))
        {
            return;
        }

        _listError = null;
        try
        {
            await EntryApi.DeleteAsync(entryId);
            _revealed.Remove(entryId);
            await LoadEntriesAsync();
        }
        catch (Exception)
        {
            _listError = "Could not delete the entry. Please try again.";
        }
    }
}
