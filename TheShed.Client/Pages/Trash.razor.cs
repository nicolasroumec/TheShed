using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Trash;

namespace TheShed.Client.Pages;

public partial class Trash
{
    [Inject] private TrashClient TrashApi { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    private IReadOnlyList<TrashItem>? _items;
    private string? _error;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _items = await TrashApi.ListAsync();
    }

    private static string DaysAgoText(DateTime deletedAt)
    {
        var days = (int)(DateTime.UtcNow - deletedAt).TotalDays;
        return days switch
        {
            0 => "today",
            1 => "1 day ago",
            _ => $"{days} days ago"
        };
    }

    private static string ExpiresText(DateTime expiresAt)
    {
        var days = (int)(expiresAt - DateTime.UtcNow).TotalDays;
        return days <= 0 ? "expires today" : $"expires in {days} day{(days == 1 ? "" : "s")}";
    }

    private async Task RestoreAsync(TrashItem item)
    {
        _error = null;
        try
        {
            await TrashApi.RestoreAsync(item.Type, item.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            _error = $"Could not restore \"{item.Name}\". Please try again.";
        }
    }

    private async Task PurgeAsync(TrashItem item)
    {
        if (!await Modal.ConfirmAsync($"Permanently delete \"{item.Name}\"? This cannot be undone.", title: "Delete forever", confirmText: "Delete forever"))
        {
            return;
        }

        _error = null;
        try
        {
            await TrashApi.PurgeAsync(item.Type, item.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            _error = $"Could not delete \"{item.Name}\". Please try again.";
        }
    }
}
