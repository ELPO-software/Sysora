using System.Runtime.InteropServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Sysora.App.Controls;

/// <summary>
/// Mica tinted with Sysora's background color (<c>SysoraBackgroundBrush</c>), so the title bar and the navigation pane
/// match the content area while keeping a hint of the wallpaper. Mica samples the wallpaper rarely, not every frame: it
/// costs nothing while the window is idle. Where Mica is not available (Windows 10, or a system that cannot provide it
/// yet just after sign-in) the window keeps its solid background instead; when transparency is turned off, Windows shows
/// the same color as a flat fallback.
/// </summary>
public sealed partial class SysoraBackdrop : SystemBackdrop
{
    private MicaController? _controller;
    private ElementTheme _theme = Application.Current.RequestedTheme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;

    /// <summary>Raised when Mica starts: the window content can then let it show through.</summary>
    public event EventHandler? Activated;

    public bool IsActive => _controller is not null;

    /// <summary>
    /// Follows the theme the window content actually uses (Sysora's setting or, for "System", Windows'). Given by the
    /// window: the backdrop configuration does not report every change of the content's requested theme.
    /// </summary>
    public void SetTheme(ElementTheme theme)
    {
        _theme = theme;
        if (_controller is not null)
        {
            ApplyTint(_controller);
        }
    }

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);

        // An exception leaving this callback would end the process: the backdrop is never worth that.
        MicaController? controller = null;
        try
        {
            if (!MicaController.IsSupported())
            {
                return;
            }

            controller = new MicaController();
            ApplyTint(controller);
            // The default configuration still tells Mica about activation and high contrast.
            controller.SetSystemBackdropConfiguration(GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot));
            controller.AddSystemBackdropTarget(connectedTarget);
        }
        catch (Exception ex) when (ex is COMException or ArgumentException or InvalidOperationException)
        {
            controller?.Dispose();
            return;
        }

        _controller = controller;
        Activated?.Invoke(this, EventArgs.Empty);
    }

    // The base implementation can fail with "target" while the window is hidden, and the tint is driven by SetTheme.
    protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
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

    private void ApplyTint(MicaController controller)
    {
        if (ThemeBrushes.Get("SysoraBackgroundBrush", _theme) is SolidColorBrush background)
        {
            controller.TintColor = background.Color;
            controller.FallbackColor = background.Color;
        }

        // Stronger tint than the system Mica: the pane must read as the same surface as the content area.
        controller.TintOpacity = _theme == ElementTheme.Dark ? 0.85f : 0.7f;
        controller.LuminosityOpacity = 1f;
    }
}
