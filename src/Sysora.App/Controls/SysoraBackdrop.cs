using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Sysora.App.Controls;

/// <summary>
/// Mica tinted with Sysora's background color (<c>SysoraBackgroundBrush</c>), so the title bar and the navigation pane
/// match the content area while keeping a hint of the wallpaper. Mica samples the wallpaper rarely, not every frame: it
/// costs nothing while the window is idle. Where Mica is not available (Windows 10) the window keeps its solid
/// background instead; when transparency is turned off, Windows shows the same color as a flat fallback.
/// </summary>
public sealed partial class SysoraBackdrop : SystemBackdrop
{
    private MicaController? _controller;
    private ElementTheme _theme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;

    public static bool IsSupported => MicaController.IsSupported();

    /// <summary>
    /// Follows the theme the window content actually uses (Sysora's setting or, for "System", Windows'). Given by the
    /// window: the backdrop configuration does not report every change of the content's requested theme.
    /// </summary>
    public void SetTheme(ElementTheme theme)
    {
        _theme = theme;
        ApplyTint();
    }

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        if (!IsSupported)
        {
            return;
        }

        _controller = new MicaController();
        ApplyTint();
        // The default configuration still tells Mica about activation and high contrast.
        _controller.SetSystemBackdropConfiguration(GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot));
        _controller.AddSystemBackdropTarget(connectedTarget);
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);
        if (_controller is not null)
        {
            _controller.RemoveSystemBackdropTarget(disconnectedTarget);
            _controller.Dispose();
            _controller = null;
        }
    }

    private void ApplyTint()
    {
        if (_controller is null)
        {
            return;
        }

        if (ThemeBrushes.Get("SysoraBackgroundBrush", _theme) is SolidColorBrush background)
        {
            _controller.TintColor = background.Color;
            _controller.FallbackColor = background.Color;
        }

        // Stronger tint than the system Mica: the pane must read as the same surface as the content area.
        _controller.TintOpacity = _theme == ElementTheme.Dark ? 0.85f : 0.7f;
        _controller.LuminosityOpacity = 1f;
    }
}
