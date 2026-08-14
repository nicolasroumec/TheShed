using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Notes;

namespace TheShed.Client.Components.Vault;

public partial class NotesPanel
{
    [Parameter] public int VaultId { get; set; }
    [Parameter] public bool CanWrite { get; set; }

    [Inject] private NoteClient NoteApi { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

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

        var note = await NoteApi.GetAsync(noteId);
        if (note is not null)
        {
            _revealedNotes[noteId] = note.Content;
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

        _editingNoteId = noteId;
        _noteError = null;
        _noteForm = new NoteCreateRequest
        {
            VaultId = VaultId,
            Title = note.Title,
            Content = note.Content,
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

        _noteBusy = true;
        _noteError = null;
        try
        {
            if (_editingNoteId is null)
            {
                await NoteApi.CreateAsync(_noteForm);
            }
            else
            {
                await NoteApi.UpdateAsync(_editingNoteId.Value, new NoteUpdateRequest
                {
                    Title = _noteForm.Title,
                    Content = _noteForm.Content,
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
