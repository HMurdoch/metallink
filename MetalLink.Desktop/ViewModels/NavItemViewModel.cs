using System.Windows.Input;

namespace MetalLink.Desktop.ViewModels;

public sealed class NavItemViewModel
{
    public required string Title { get; init; }

    /// <summary>
    /// Icon key for Projektanker icons. For FontAwesome provider, use "fa-solid fa-...".
    /// </summary>
    public required string IconKey { get; init; }

    public required ICommand Command { get; init; }

    public bool IsHeader { get; init; }

    public bool IsIndented { get; init; }
}
