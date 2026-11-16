using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Julemesselotteri.App;

public sealed class DigitReel : Border
{
    private readonly TextBlock _previous = CreateNumber();
    private readonly TextBlock _current = CreateNumber();
    private char _digit = ' ';

    public DigitReel()
    {
        Width = 152;
        Height = 220;
        Margin = new Thickness(5, 0, 5, 0);
        CornerRadius = new CornerRadius(14);
        BorderThickness = new Thickness(1);
        BorderBrush = new SolidColorBrush(Color.FromArgb(110, 231, 197, 128));
        Background = new LinearGradientBrush(Color.FromRgb(45, 70, 56), Color.FromRgb(20, 43, 35), 90);
        var slot = new Grid { ClipToBounds = true, Margin = new Thickness(0, 4, 0, 4) };
        slot.Children.Add(_previous);
        slot.Children.Add(_current);
        Child = slot;
    }

    private static TextBlock CreateNumber() => new()
    {
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 148,
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(255, 227, 166)),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        RenderTransform = new TranslateTransform()
    };

    public void SetDigit(char digit, bool animate, double interval)
    {
        if (digit == _digit && animate) return;
        var old = _digit;
        _digit = digit;
        var previousTransform = (TranslateTransform)_previous.RenderTransform;
        var currentTransform = (TranslateTransform)_current.RenderTransform;
        previousTransform.BeginAnimation(TranslateTransform.YProperty, null);
        currentTransform.BeginAnimation(TranslateTransform.YProperty, null);
        _previous.Text = animate ? old.ToString() : "";
        _current.Text = digit.ToString();
        previousTransform.Y = 0;
        currentTransform.Y = 0;
        if (!animate) return;
        var duration = TimeSpan.FromMilliseconds(Math.Clamp(interval * 0.85, 25, 250));
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        previousTransform.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(0, 220, duration) { EasingFunction = easing });
        currentTransform.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(-220, 0, duration) { EasingFunction = easing });
    }
}
