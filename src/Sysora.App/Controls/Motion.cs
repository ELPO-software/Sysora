using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;

namespace Sysora.App.Controls;

/// <summary>
/// Discreet motion for panels that appear (details, sections, empty states): a 200 ms fade with an 8 px rise, run by
/// the compositor when the element is shown. Nothing runs while the panel stays on screen, so live updates cost nothing
/// extra. Not applied when animations are turned off in Windows (Accessibility › Visual effects).
/// </summary>
public static class Motion
{
    public static readonly DependencyProperty RevealOnShowProperty = DependencyProperty.RegisterAttached(
        "RevealOnShow", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnRevealOnShowChanged));

    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(200);
    private const float Rise = 8;
    private static readonly UISettings Settings = new();
    private static CompositionAnimationGroup? _show;

    public static bool GetRevealOnShow(UIElement element) => (bool)element.GetValue(RevealOnShowProperty);

    public static void SetRevealOnShow(UIElement element, bool value) => element.SetValue(RevealOnShowProperty, value);

    private static void OnRevealOnShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element || e.NewValue is not true || !AnimationsEnabled())
        {
            return;
        }

        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        ElementCompositionPreview.SetImplicitShowAnimation(element, _show ??= CreateShowAnimation(ElementCompositionPreview.GetElementVisual(element).Compositor));
    }

    private static CompositionAnimationGroup CreateShowAnimation(Compositor compositor)
    {
        // Decelerating curve: quick start, gentle landing.
        var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.Target = "Opacity";
        fade.InsertKeyFrame(0, 0);
        fade.InsertKeyFrame(1, 1, easing);
        fade.Duration = Duration;

        var rise = compositor.CreateVector3KeyFrameAnimation();
        rise.Target = "Translation";
        rise.InsertKeyFrame(0, new Vector3(0, Rise, 0));
        rise.InsertKeyFrame(1, Vector3.Zero, easing);
        rise.Duration = Duration;

        var group = compositor.CreateAnimationGroup();
        group.Add(fade);
        group.Add(rise);
        return group;
    }

    private static bool AnimationsEnabled()
    {
        try
        {
            return Settings.AnimationsEnabled;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            return true;
        }
    }
}
