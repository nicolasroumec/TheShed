using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Attachments;

namespace TheShed.Client.Components.Vault;

public partial class EntryAttachmentsPanel
{
    [Parameter, EditorRequired] public int EntryId { get; set; }
    [Parameter] public bool CanWrite { get; set; }
    [Parameter] public bool IsOpen { get; set; }

    [Inject] private AttachmentClient AttachmentApi { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private IReadOnlyList<AttachmentResponse>? _attachments;
    private bool _busy;
    private string? _error;

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && _attachments is null)
        {
            _attachments = await AttachmentApi.ListAsync(EntryId);
        }
    }

    private static string FileSizeText(long bytes) => bytes < 1024 * 1024
        ? $"{bytes / 1024.0:0.#} KB"
        : $"{bytes / 1024.0 / 1024.0:0.#} MB";

    private async Task UploadAttachmentAsync(InputFileChangeEventArgs e)
    {
        _error = null;
        _busy = true;
        try
        {
            var response = await AttachmentApi.UploadAsync(EntryId, e.File);
            if (!response.IsSuccessStatusCode)
            {
                _error = "Could not upload the file. Check the size (max 5 MB) and type (.pdf, .jpg, .jpeg, .png, .txt).";
                return;
            }
            _attachments = await AttachmentApi.ListAsync(EntryId);
        }
        catch (Exception)
        {
            _error = "Could not upload the file. Please try again.";
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DownloadAttachmentAsync(AttachmentResponse file)
    {
        var (fileName, content) = await AttachmentApi.DownloadAsync(EntryId, file.Id);
        await JS.InvokeVoidAsync("downloadFile", fileName, content);
    }

    private async Task DeleteAttachmentAsync(int attachmentId)
    {
        if (!await JS.InvokeAsync<bool>("confirm", "Delete this file? This cannot be undone."))
        {
            return;
        }

        _error = null;
        try
        {
            await AttachmentApi.DeleteAsync(EntryId, attachmentId);
            _attachments = await AttachmentApi.ListAsync(EntryId);
        }
        catch (Exception)
        {
            _error = "Could not delete the file. Please try again.";
        }
    }
}
