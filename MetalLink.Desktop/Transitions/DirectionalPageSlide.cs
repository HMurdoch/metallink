using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.VisualTree;

namespace MetalLink.Desktop.Transitions;

public class DirectionalPageSlide : AvaloniaObject, IPageTransition
{
    public static readonly StyledProperty<bool> IsForwardProperty =
        AvaloniaProperty.Register<DirectionalPageSlide, bool>(
            nameof(IsForward), true);

    /// <summary>
    /// True = slide like "next page"
    /// False = slide like "previous page"
    /// </summary>
    public bool IsForward
    {
        get => GetValue(IsForwardProperty);
        set => SetValue(IsForwardProperty, value);
    }

    /// <summary>
    /// Duration of the animation.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(250);

    // Internal Avalonia slide transition
    private readonly PageSlide _slide = new PageSlide();

    public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        // Set duration
        _slide.Duration = Duration;

        // Override Avalonia's direction with our own
        var effectiveForward = IsForward;

        return _slide.Start(from, to, effectiveForward, cancellationToken);
    }
}
