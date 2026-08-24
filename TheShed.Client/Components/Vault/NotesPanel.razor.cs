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
        _notes = await NoteApi.ListAsync(VaultId);
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
            Title = note.Title,
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
            // Encrypted into a local var, not written back onto _noteForm.Content: a failed save
            // leaves the form open for retry, and the visible textarea should stay the plaintext
            // the user typed, not the ciphertext blob from the failed attempt.
            var encryptedContent = await AesGcm.EncryptAsync(vaultKey, _noteForm.Content);

            if (_editingNoteId is null)
            {
                await NoteApi.CreateAsync(new NoteCreateRequest
                {
                    VaultId = _noteForm.VaultId,
                    Title = _noteForm.Title,
                    Content = encryptedContent,
                    IsFavorite = _noteForm.IsFavorite
                });
            }
            else
            {
                await NoteApi.UpdateAsync(_editingNoteId.Value, new NoteUpdateRequest
                {
                    Title = _noteForm.Title,
                    Content = encryptedContent,
                    IsFavorite = _noteForm.IsFavorite
                });
                _revealedNotes.Remove(_editingNoteId.Value); // stale content if it was shown
            }

            CancelNoteForm();
            _notes = await NoteApi.ListAsync(VaultId);
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
            _notes = await NoteApi.ListAsync(VaultId);
        }
        catch (Exception)
        {
            _notesListError = "Could not delete the note. Please try again.";
        }
    }
}
