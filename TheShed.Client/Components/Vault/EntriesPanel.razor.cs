using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TheShed.Client.Services;
using TheShed.Shared.Helpers;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Models.DTOs.Tags;

namespace TheShed.Client.Components.Vault;

public partial class EntriesPanel : IDisposable
{
    [Parameter] public int VaultId { get; set; }
    [Parameter] public bool CanWrite { get; set; }

    [Inject] private EntryClient EntryApi { get; set; } = default!;
    [Inject] private TagClient TagApi { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    private IReadOnlyList<EntryListItem>? _entries;
    private string? _listError;     // failures of list-level actions (favorite, delete)
    private CancellationTokenSource? _searchCts;

    private EntryCreateRequest? _form;   // non-null while the create/edit form is open
    private int? _editingId;             // null = creating, otherwise the entry being edited
    private bool _busy;
    private string? _error;

    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(300);

    private bool _showPassword;        // toggles the form password field between text/password
    private bool _genMemorable;        // false = random chars, true = passphrase
    private int _genLength = 20;       // random mode options
    private bool _genSymbols = true;
    private int _genWordCount = 4;     // memorable mode options
    private string _genSeparator = "-";

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
        StateHasChanged(); // called externally via @ref, so no automatic re-render follows
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
        if (!await Modal.ConfirmAsync($"Remove the tag \"{tag.Name}\"? It will be removed from all entries.", title: "Remove tag", confirmText: "Remove"))
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

        _form.Password = _genMemorable
            ? PasswordGenerator.GeneratePassphrase(Math.Clamp(_genWordCount, 1, 10), _genSeparator)
            : PasswordGenerator.Generate(Math.Clamp(_genLength, 4, 128), _genSymbols); // clamp guards out-of-range typed values
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
                // Stale reveal/history caches are now handled by EntryRow, which invalidates
                // them when the entry's PasswordChangedAt changes under it (see OnParametersSet).
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

    /// <summary>Bound to EntryRow's OnDeleteRequested — the row already confirmed with the user
    /// before invoking this, so it isn't repeated here.</summary>
    private async Task DeleteAsync(int entryId)
    {
        _listError = null;
        try
        {
            await EntryApi.DeleteAsync(entryId);
            await LoadEntriesAsync();
        }
        catch (Exception)
        {
            _listError = "Could not delete the entry. Please try again.";
        }
    }
}
