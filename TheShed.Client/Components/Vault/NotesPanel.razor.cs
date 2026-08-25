using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Notes;
using TheShed.Shared.Security;

namespace TheShed.Client.Components.Vault;

public partial class NotesPanel
{
    [Parameter] public int VaultId { get; set; }
    [Parameter] public bool CanWrite { get; set; }

    [Inject] private NoteClient NoteApi { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IVaultKeyCache VaultKeyCache { get; set; } = default!;
    [Inject] private IAesGcmService AesGcm { get; set; } = default!;

    private const string MissingVaultKeyError = "Your session is missing this vault's encryption key — log out and log back in.";

    private IReadOnlyList<NoteListItem>? _notes;
    private readonly Dictionary<int, string> _revealedNotes = new();
    private NoteCreateRequest? _noteForm;  // non-null while the note create/edit form is open
    private int? _editingNoteId;           // null = creating, otherwise the note being edited
    private bool _noteBusy;
    private string? _noteError;
    private string? _notesListError; // failures of list-level actions (delete)

    protected override async Task OnInitializedAsync()
    {
        await LoadNotesAsync();
    }

    /// <summary>Fetches the list from the server and decrypts Title in place — the server can no
    /// longer sort by it (Sprint 26 ciphertext), so it returns notes unordered and the caller
    /// re-sorts favorite-then-title after decrypting, same as EntriesPanel.</summary>
    private async Task LoadNotesAsync()
    {
        var vaultKey = VaultKeyCache.Get(VaultId);
        if (vaultKey is null)
        {
            _notesListError = MissingVaultKeyError;
            _notes = [];
            return;
        }

        var fetched = await NoteApi.ListAsync(VaultId);
        foreach (var note in fetched)
        {
            note.Title = await AesGcm.DecryptAsync(vaultKey, note.Title);
        }

        _notes = fetched
            .OrderByDescending(n => n.IsFavorite)
            .ThenBy(n => n.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task ToggleRevealNoteAsync(int noteId)
    {
        if (_revealedNotes.Remove(noteId))
        {
            return; // was shown, now hidden
        }

        var vaultKey = VaultKeyCache.Get(VaultId);
        if (vaultKey is null)
        {
            _notesListError = MissingVaultKeyError;
            return;
        }

        var note = await NoteApi.GetAsync(noteId);
        if (note is not null)
        {
            _notesListError = null;
            _revealedNotes[noteId] = await AesGcm.DecryptAsync(vaultKey, note.Content);
        }
    }

    private void StartCreateNote()
    {
        _editingNoteId = null;
        _noteError = null;
        _noteForm = new NoteCreateRequest { VaultId = VaultId };
    }

    private async Task StartEditNoteAsync(int noteId)
    {
        var note = await NoteApi.GetAsync(noteId);
        if (note is null)
        {
            return;
        }

        var vaultKey = VaultKeyCache.Get(VaultId);
        if (vaultKey is null)
        {
            _noteError = MissingVaultKeyError;
            return;
        }

        _editingNoteId = noteId;
        _noteError = null;
        _noteForm = new NoteCreateRequest
        {
            VaultId = VaultId,
            Title = await AesGcm.DecryptAsync(vaultKey, note.Title),
            Content = await AesGcm.DecryptAsync(vaultKey, note.Content),
            IsFavorite = note.IsFavorite
        };
    }

    private void CancelNoteForm()
    {
        _noteForm = null;
        _editingNoteId = null;
        _noteError = null;
    }

    private async Task SubmitNoteAsync()
    {
        if (_noteForm is null)
        {
            return;
        }

        var vaultKey = VaultKeyCache.Get(VaultId);
        if (vaultKey is null)
        {
            _noteError = MissingVaultKeyError;
            return;
        }

        _noteBusy = true;
        _noteError = null;
        try
        {
            // Encrypted into local vars, not written back onto _noteForm: a failed save leaves
            // the form open for retry, and the visible inputs should stay the plaintext the user
            // typed, not the ciphertext blobs from the failed attempt.
            var encryptedTitle = await AesGcm.EncryptAsync(vaultKey, _noteForm.Title);
            var encryptedContent = await AesGcm.EncryptAsync(vaultKey, _noteForm.Content);

            if (_editingNoteId is null)
            {
                await NoteApi.CreateAsync(new NoteCreateRequest
                {
                    VaultId = _noteForm.VaultId,
                    Title = encryptedTitle,
                    Content = encryptedContent,
                    IsFavorite = _noteForm.IsFavorite
                });
            }
            else
            {
                await NoteApi.UpdateAsync(_editingNoteId.Value, new NoteUpdateRequest
                {
                    Title = encryptedTitle,
                    Content = encryptedContent,
                    IsFavorite = _noteForm.IsFavorite
                });
                _revealedNotes.Remove(_editingNoteId.Value); // stale content if it was shown
            }

            CancelNoteForm();
            await LoadNotesAsync();
        }
        catch (Exception)
        {
            _noteError = "Could not save the note. Please try again.";
        }
        finally
        {
            _noteBusy = false;
        }
    }

    private async Task DeleteNoteAsync(int noteId)
    {
        if (!await Modal.ConfirmAsync("Delete this note? This cannot be undone.", title: "Delete note", confirmText: "Delete"))
        {
            return;
        }

        _notesListError = null;
        try
        {
            await NoteApi.DeleteAsync(noteId);
            _revealedNotes.Remove(noteId);
            await LoadNotesAsync();
        }
        catch (Exception)
        {
            _notesListError = "Could not delete the note. Please try again.";
        }
    }
}
