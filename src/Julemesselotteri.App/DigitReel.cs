using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Julemesselotteri.App;

public sealed class DigitReel : Border
{
    private readonly TextBlock _previous = CreateNumber();
    private readonly TextBlock _current = CreateNumber();

    public DigitReel()
    {
        Width = 152;
        Height = 220;
        Margin = new Thickness(5, 0, 5, 0);
        CornerRadius = new CornerRadius(4);
        BorderThickness = new Thickness(0, 0, 0, 6);
        BorderBrush = (Brush)Application.Current.FindResource("Accent");
        Background = (Brush)Application.Current.FindResource("BrandCream");
        var slot = new Grid { ClipToBounds = true, Margin = new Thickness(0, 4, 0, 4) };
        slot.Children.Add(_previous);
        slot.Children.Add(_current);
        Child = slot;
    }

    private static TextBlock CreateNumber() => new()
    {
        FontFamily = (FontFamily)Application.Current.FindResource("BrandFont"),
        FontSize = 148,
        FontWeight = FontWeights.Bold,
        Foreground = (Brush)Application.Current.FindResource("BrandDark"),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        RenderTransform = new TranslateTransform()
    };

    public void SetDigit(char digit)
    {
        SetGlyph(_previous, digit);
        _current.Text = "";
        ((TranslateTransform)_previous.RenderTransform).Y = 0;
    }

    public char SetSpinPosition(double position)
    {
        var index = (int)Math.Floor(position);
        var fraction = position - index;
        var digit = (index % 10 + 10) % 10;
        var next = (digit + 1) % 10;
        SetGlyph(_previous, (char)('0' + digit));
        SetGlyph(_current, (char)('0' + next));
        ((TranslateTransform)_previous.RenderTransform).Y = fraction * Height;
        ((TranslateTransform)_current.RenderTransform).Y = (fraction - 1) * Height;
        return (char)('0' + (fraction < 0.5 ? digit : next));
    }

    private static void SetGlyph(TextBlock text, char digit)
    {
        if (text.Text.Length == 0 || text.Text[0] != digit) text.Text = digit.ToString();
    }
}
