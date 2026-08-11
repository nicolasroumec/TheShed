using Microsoft.AspNetCore.Components;

namespace TheShed.Client.Components;

public partial class Loading
{
    [Parameter] public string Text { get; set; } = "Loading…";

    /// <summary>Extra classes for inline use, e.g. "small mb-0" inside a panel.</summary>
    [Parameter] public string? Class { get; set; }
}
