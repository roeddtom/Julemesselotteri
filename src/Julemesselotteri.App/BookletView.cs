using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Julemesselotteri.Core;

namespace Julemesselotteri.App;

public sealed class BookletView : Grid
{
    private static readonly Lazy<BitmapSource[]> Handwriting = new(() =>
        Enumerable.Range(1, 40).Select(index =>
        {
            var image = new BitmapImage(new Uri(
                $"pack://application:,,,/Julemesselotteri.App;component/Assets/book-rows/rad_{index:00}.png"));
            image.Freeze();
            return (BitmapSource)image;
        }).ToArray());
    private readonly Border _cover;
    private readonly Border _paper;
    private readonly StackPanel _rows = new();
    private readonly TextBlock _coverLabel = new() { FontSize = 19, TextAlignment = TextAlignment.Right };
    private readonly TextBlock _winner = new() { FontSize = 72, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center };
    private readonly ScaleTransform _turn = new(1, 1);
    private int _book;
    private int _page;
    private int[] _rowImages = [];
    public int Page => _page;
    public int? HighlightedNumber { get; private set; }

    public BookletView()
    {
        const double pageWidth = 240;
        Width = pageWidth * 2;
        Height = pageWidth * 210 / 148;
        var ink = (Brush)Application.Current.FindResource("BrandDark");
        var cream = (Brush)Application.Current.FindResource("BrandCream");
        TextElement.SetForeground(this, ink);
        _cover = new Border
        {
            Width = pageWidth,
            Background = cream,
            Padding = new Thickness(10),
            RenderTransformOrigin = new Point(0, 0.5),
            RenderTransform = new ScaleTransform(1, 1)
        };
        var coverContent = new Grid();
        coverContent.RowDefinitions.Add(new RowDefinition());
        coverContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        coverContent.Children.Add(new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/Julemesselotteri.App;component/Assets/book-cover.png")),
            Stretch = Stretch.Uniform
        });
        Grid.SetRow(_coverLabel, 1);
        coverContent.Children.Add(_coverLabel);
        _cover.Child = coverContent;

        var spread = new Grid();
        spread.ColumnDefinitions.Add(new ColumnDefinition());
        spread.ColumnDefinitions.Add(new ColumnDefinition());
        var summary = new StackPanel { Margin = new Thickness(12), VerticalAlignment = VerticalAlignment.Center };
        summary.Children.Add(new Viewbox { Child = _winner, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly });
        spread.Children.Add(summary);
        var spine = new Border { Width = 2, HorizontalAlignment = HorizontalAlignment.Center, Background = (Brush)Application.Current.FindResource("BrandBorder") };
        Grid.SetColumnSpan(spine, 2);
        spread.Children.Add(spine);
        var table = new StackPanel
        {
            Margin = new Thickness(10),
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = _turn,
            RenderTransformOrigin = new Point(0, 0.5)
        };
        table.Children.Add(CreateRow("Loddnr", "Navn", "Telefon", true));
        table.Children.Add(_rows);
        Grid.SetColumn(table, 1);
        spread.Children.Add(table);
        _paper = new Border { Background = cream, Child = spread, BorderBrush = ink, BorderThickness = new Thickness(1) };
        Children.Add(_paper);
        Children.Add(_cover);
        Visibility = Visibility.Collapsed;
    }

    private static Grid CreateRow(string ticket, string name, string phone, bool heading = false,
        BitmapSource? handwriting = null)
    {
        var grid = new Grid { Height = heading ? 24 : 25 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        var values = new[] { ticket, name, phone };
        for (var i = 0; i < values.Length; i++)
        {
            var border = new Border
            {
                BorderBrush = (Brush)Application.Current.FindResource("BrandBorder"),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new TextBlock { Text = values[i], FontSize = 13, Padding = new Thickness(4, 2, 0, 0), FontWeight = heading ? FontWeights.Bold : FontWeights.Normal }
            };
            Grid.SetColumn(border, i);
            grid.Children.Add(border);
        }
        if (handwriting is not null)
        {
            var image = new Image
            {
                Source = handwriting,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(3, 1, 3, 1),
                IsHitTestVisible = false
            };
            Grid.SetColumn(image, 1);
            Grid.SetColumnSpan(image, 2);
            grid.Children.Add(image);
        }
        return grid;
    }

    private void SetBook(int book)
    {
        if (_book == book) return;
        _book = book;
        _rowImages = Enumerable.Range(0, 50).Select(_ => Random.Shared.Next(Handwriting.Value.Length)).ToArray();
    }

    public void ShowCover(DrawRecord draw, int totalBooks)
    {
        Visibility = Visibility.Visible;
        SetBook(draw.Book);
        _page = 0;
        HighlightedNumber = null;
        _winner.Text = "";
        _paper.Visibility = Visibility.Hidden;
        _cover.Visibility = Visibility.Visible;
        _coverLabel.Text = $"Lodd {(_book - 1) * 50 + 1}–{_book * 50}\nBok {_book}/{totalBooks}";
        _cover.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
        ((ScaleTransform)_cover.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, null);
    }

    public void ShowPage(int page, bool animate)
    {
        if (_page == page && _paper.Visibility == Visibility.Visible) return;
        var opening = _cover.Visibility == Visibility.Visible;
        _page = page;
        HighlightedNumber = null;
        _paper.Visibility = Visibility.Visible;
        _rows.Children.Clear();
        for (var row = 1; row <= 10; row++)
            _rows.Children.Add(CreateRow(((_book - 1) * 50 + (page - 1) * 10 + row).ToString(), "", "",
                handwriting: Handwriting.Value[_rowImages[(page - 1) * 10 + row - 1]]));
        _turn.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        if (animate)
        {
            _turn.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.05, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
        if (opening && animate)
        {
            var coverTurn = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
            coverTurn.Completed += (_, _) => _cover.Visibility = Visibility.Hidden;
            ((ScaleTransform)_cover.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, coverTurn);
        }
        else _cover.Visibility = Visibility.Hidden;
    }

    public void Reveal(DrawRecord draw)
    {
        if (_book != draw.Book || HighlightedNumber != draw.Number) _page = 0;
        SetBook(draw.Book);
        var local = (draw.Number - 1) % 50;
        ShowPage(local / 10 + 1, false);
        _cover.Visibility = Visibility.Hidden;
        _turn.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        var row = (Grid)_rows.Children[local % 10];
        row.Background = (Brush)Application.Current.FindResource("Accent");
        TextElement.SetForeground(row, (Brush)Application.Current.FindResource("BrandCream"));
        HighlightedNumber = draw.Number;
        _winner.Inlines.Clear();
        _winner.Inlines.Add(new Run("Vinner\n"));
        _winner.Inlines.Add(new Run(draw.Number.ToString()) { FontSize = 104 });
        Visibility = Visibility.Visible;
    }
}
