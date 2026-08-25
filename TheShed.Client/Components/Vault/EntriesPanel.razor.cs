using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TheShed.Client.Services;
using TheShed.Shared.Helpers;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Models.DTOs.Tags;
using TheShed.Shared.Security;

namespace TheShed.Client.Components.Vault;

public partial class EntriesPanel
{
    [Parameter] public int VaultId { get; set; }
    [Parameter] public bool CanWrite { get; set; }

    [Inject] private EntryClient EntryApi { get; set; } = default!;
    [Inject] private TagClient TagApi { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IVaultKeyCache VaultKeyCache { get; set; } = default!;
    [Inject] private IAesGcmService AesGcm { get; set; } = default!;

    private byte[]? VaultKey => VaultKeyCache.Get(VaultId);
    private const string MissingVaultKeyError = "Your session is missing this vault's encryption key — log out and log back in.";

    // Everything for the current tag filter, decrypted right after fetching (Sprint 26: the
    // server can no longer search/sort Name/Username/Url, they're ciphertext). _entries is the
    // subset of _allEntries that also matches _search, re-sorted — what the markup renders.
    private IReadOnlyList<EntryListItem>? _allEntries;
    private IReadOnlyList<EntryListItem>? _entries;
    private string? _listError;     // failures of list-level actions (favorite, delete, load)

    private EntryCreateRequest? _form;   // non-null while the create/edit form is open
    private int? _editingId;             // null = creating, otherwise the entry being edited
    private string? _editingOriginalPassword; // plaintext as loaded, to detect a real change on save
    private bool _busy;
    private string? _error;

    private bool _showPassword;        // toggles the form password field between text/password
    private bool _genMemorable;        // false = random chars, true = passphrase
    private int _genLength = 20;       // random mode options
    private bool _genSymbols = true;
    private int _genWordCount = 4;     // memorable mode options
    private string _genSeparator = "-";

    private IReadOnlyList<TagResponse> _tags = [];      // the caller's tags (per-user)
    private int? _activeTagId;                          // null = no tag filter (still server-side: TagId isn't encrypted)
    private string _search = string.Empty;               // empty = no search filter (client-side only)
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

        var vaultKey = VaultKey;
        if (vaultKey is null)
        {
            _error = MissingVaultKeyError;
            return;
        }

        var plaintextName = await AesGcm.DecryptAsync(vaultKey, entry.Name);
        var plaintextUsername = await AesGcm.DecryptAsync(vaultKey, entry.Username);
        var plaintextUrl = string.IsNullOrEmpty(entry.Url) ? entry.Url : await AesGcm.DecryptAsync(vaultKey, entry.Url);
        var plaintextPassword = await AesGcm.DecryptAsync(vaultKey, entry.Password);
        var plaintextNotes = string.IsNullOrEmpty(entry.Notes) ? entry.Notes : await AesGcm.DecryptAsync(vaultKey, entry.Notes);

        _editingId = entryId;
        _error = null;
        _editingOriginalPassword = plaintextPassword;
        _formTagIds.Clear();
        foreach (var tag in entry.Tags)
        {
            _formTagIds.Add(tag.Id);
        }
        _form = new EntryCreateRequest
        {
            VaultId = VaultId,
            Name = plaintextName,
            Username = plaintextUsername,
            Password = plaintextPassword,
            Url = plaintextUrl,
            Notes = plaintextNotes,
            IsFavorite = entry.IsFavorite
        };
    }

    private void CancelForm()
    {
        _form = null;
        _editingId = null;
        _editingOriginalPassword = null;
        _error = null;
        _showPassword = false;
        _formTagIds.Clear();
    }

    /// <summary>Fetches the (tag-filtered) list from the server and decrypts Name/Username/Url
    /// in place — EntryListItem instances here are client-owned from this point, never re-sent.
    /// Recomputes the visible (search-filtered, sorted) list afterwards.</summary>
    private async Task LoadEntriesAsync()
    {
        var vaultKey = VaultKey;
        if (vaultKey is null)
        {
            _listError = MissingVaultKeyError;
            _allEntries = [];
            RecomputeVisible();
            return;
        }

        var fetched = await EntryApi.ListAsync(VaultId, _activeTagId);
        foreach (var entry in fetched)
        {
            entry.Name = await AesGcm.DecryptAsync(vaultKey, entry.Name);
            entry.Username = await AesGcm.DecryptAsync(vaultKey, entry.Username);
            if (!string.IsNullOrEmpty(entry.Url))
            {
                entry.Url = await AesGcm.DecryptAsync(vaultKey, entry.Url);
            }
        }

        _listError = null;
        _allEntries = fetched;
        RecomputeVisible();
    }

    /// <summary>Applies the search filter and the favorite-then-name sort over the already
    /// decrypted _allEntries, entirely in memory — no server round trip (Sprint 26).</summary>
    private void RecomputeVisible()
    {
        IEnumerable<EntryListItem> visible = _allEntries ?? [];
        if (!string.IsNullOrWhiteSpace(_search))
        {
            visible = visible.Where(e =>
                e.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                e.Username.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                (e.Url is not null && e.Url.Contains(_search, StringComparison.OrdinalIgnoreCase)));
        }

        _entries = visible
            .OrderByDescending(e => e.IsFavorite)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Search-as-you-type: now a plain in-memory filter (Sprint 26 moved Name/Username/Url
    /// off the server), so it runs instantly on every keystroke — no debounce needed.</summary>
    private void SearchInput(ChangeEventArgs e)
    {
        _search = (string?)e.Value ?? string.Empty;
        RecomputeVisible();
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

        var vaultKey = VaultKey;
        if (vaultKey is null)
        {
            _error = MissingVaultKeyError;
            return;
        }

        _busy = true;
        _error = null;
        try
        {
            // Encrypted into local vars, not written back onto _form: a failed save leaves the
            // form open for retry, and the visible inputs should stay the plaintext the user
            // typed, not the ciphertext blobs from the failed attempt.
            var encryptedName = await AesGcm.EncryptAsync(vaultKey, _form.Name);
            var encryptedUsername = await AesGcm.EncryptAsync(vaultKey, _form.Username);
            var encryptedUrl = string.IsNullOrEmpty(_form.Url) ? _form.Url : await AesGcm.EncryptAsync(vaultKey, _form.Url);
            var encryptedPassword = await AesGcm.EncryptAsync(vaultKey, _form.Password);
            var encryptedNotes = string.IsNullOrEmpty(_form.Notes) ? _form.Notes : await AesGcm.EncryptAsync(vaultKey, _form.Notes);

            if (_editingId is null)
            {
                await EntryApi.CreateAsync(new EntryCreateRequest
                {
                    VaultId = _form.VaultId,
                    Name = encryptedName,
                    Username = encryptedUsername,
                    Password = encryptedPassword,
                    Url = encryptedUrl,
                    Notes = encryptedNotes,
                    IsFavorite = _form.IsFavorite
                });
            }
            else
            {
                await EntryApi.UpdateAsync(_editingId.Value, new EntryUpdateRequest
                {
                    Name = encryptedName,
                    Username = encryptedUsername,
                    Password = encryptedPassword,
                    PasswordChanged = _form.Password != _editingOriginalPassword,
                    Url = encryptedUrl,
                    Notes = encryptedNotes,
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
