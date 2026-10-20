namespace PodShift;

/// <summary>
/// Tiny set of user settings persisted between launches.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Last widget X position in WPF device-independent units; null until the widget has been placed.</summary>
    public double? Left { get; set; }

    /// <summary>Last widget Y position in WPF device-independent units; null until the widget has been placed.</summary>
    public double? Top { get; set; }

    /// <summary>Whether the widget stays above other windows.</summary>
    public bool Topmost { get; set; }
}
