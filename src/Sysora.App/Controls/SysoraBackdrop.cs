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
    private SystemBackdropConfiguration? _configuration;

    public static bool IsSupported => MicaController.IsSupported();

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        if (!IsSupported)
        {
            return;
        }

        // The default configuration follows the theme of the window content (and high contrast, activation).
        _configuration = GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
        _controller = new MicaController();
        ApplyTint();
        _controller.SetSystemBackdropConfiguration(_configuration);
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

    protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);
        ApplyTint();
    }

    private void ApplyTint()
    {
        if (_controller is null || _configuration is null)
        {
            return;
        }

        var theme = _configuration.Theme switch
        {
            SystemBackdropTheme.Dark => ElementTheme.Dark,
            SystemBackdropTheme.Light => ElementTheme.Light,
            _ => Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light,
        };

        if (ThemeBrushes.Get("SysoraBackgroundBrush", theme) is SolidColorBrush background)
        {
            _controller.TintColor = background.Color;
            _controller.FallbackColor = background.Color;
        }

        // Stronger tint than the system Mica: the pane must read as the same surface as the content area.
        _controller.TintOpacity = theme == ElementTheme.Dark ? 0.85f : 0.7f;
        _controller.LuminosityOpacity = 1f;
    }
}
