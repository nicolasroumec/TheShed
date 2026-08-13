using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Helpers;
using TheShed.Shared.Models.DTOs.Health;

namespace TheShed.Client.Pages;

public partial class Health
{
    [Inject] private HealthClient HealthApi { get; set; } = default!;

    private IReadOnlyList<PasswordHealthItem>? _items;

    protected override async Task OnInitializedAsync()
    {
        _items = await HealthApi.GetPasswordReportAsync();
    }

    private static string StrengthChipClass(PasswordStrength strength) => strength switch
    {
        PasswordStrength.Weak => "strength-weak",
        PasswordStrength.Medium => "strength-medium",
        _ => "strength-strong"
    };
}
