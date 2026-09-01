using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Entries;

namespace TheShed.Client.Components.Vault;

public partial class ImportExportPanel
{
    [Parameter, EditorRequired] public int VaultId { get; set; }
    [Parameter] public bool CanWrite { get; set; }

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // mirrors AttachmentClient's cap

    [Inject] private ImportExportClient ImportExportApi { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    private ImportFormat _format = ImportFormat.LastPass;
    private bool _importing;
    private bool _exporting;
    private ImportResult? _result;
    private string? _error;

    private async Task ImportAsync(InputFileChangeEventArgs e)
    {
        _error = null;
        _result = null;
        _importing = true;
        try
        {
            using var stream = e.File.OpenReadStream(MaxFileSizeBytes);
            _result = await ImportExportApi.ImportAsync(stream, _format, VaultId);
        }
        catch (Exception)
        {
            _error = "Could not read the file. Check it's a CSV under 5 MB.";
        }
        finally
        {
            _importing = false;
        }
    }

    private async Task ExportAsync()
    {
        if (!await Modal.ConfirmAsync(
            "The exported file will contain every password in plain text. Anyone who opens it can read them.",
            title: "Export entries",
            confirmText: "Export"))
        {
            return;
        }

        _error = null;
        _exporting = true;
        try
        {
            var csvBytes = await ImportExportApi.ExportAsync(VaultId);
            await JS.InvokeVoidAsync("downloadFile", "entries-export.csv", csvBytes);
        }
        catch (Exception)
        {
            _error = "Could not export the vault. Please try again.";
        }
        finally
        {
            _exporting = false;
        }
    }
}
