using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace MetalLink.Desktop.Views;

/// <summary>
/// A custom DataGrid that applies fade-in animation to rows when they appear.
/// Simple implementation using opacity transitions.
/// </summary>
public class AnimatedDataGrid : DataGrid
{
    public AnimatedDataGrid()
    {
        // Listen for when items are loaded
        this.LayoutUpdated += OnLayoutUpdated;
    }

    private bool _isAnimating;

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        // Animate rows on next dispatcher cycle
        _ = Dispatcher.UIThread.InvokeAsync(AnimateRows, DispatcherPriority.Loaded);
    }

    private void AnimateRows()
    {
        try
        {
            // Set all visible DataGrid rows to fade in
            // We'll just set opacity to trigger the transition
            this.Opacity = 0.7; // Slight fade for visual effect
            
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.Opacity = 1;
            }, DispatcherPriority.Loaded);
        }
        finally
        {
            _isAnimating = false;
        }
    }
}
