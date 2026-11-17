using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Julemesselotteri.App;
using Julemesselotteri.Core;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
        var root = Path.Combine(Path.GetTempPath(), "Julemesselotteri-ui-" + Guid.NewGuid());
        var app = new App();
        app.InitializeComponent();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        try
        {
            if (args.FirstOrDefault() == "--headless-booklet")
            {
                CheckBookletRows();
                CheckBookletLayout(args.Skip(1).FirstOrDefault());
                CheckBookTicketAnimation();
                CheckAnimationSpeed();
                Console.WriteLine("PASS: portrait booklet layout, ticket mapping and reel animations; no windows shown.");
                return 0;
            }
            CheckDisplayPlacement();
            var store = new EventFileStore(root, Path.Combine(root, "logs"));
            var engine = new LotteryEngine(store, _ => 0);
            engine.SaveSettings(new AppSettings
            {
                HeartPrizes = "Hovedgevinst\nSjokolade",
                BookPrizes = "Sykkel",
                Books = new() { FirstTicket = 73, LastTicket = 125, UnsoldTickets = "74-100" },
                HeartsAnimation = new() { DurationSeconds = 10, TickMilliseconds = 30 },
                BooksAnimation = new() { DurationSeconds = 0.5, TickMilliseconds = 40, BookPauseSeconds = 0.4 },
                Muted = true
            });
            var approve = true;
            var window = new MainWindow(engine, store, (_, _) => approve);
            window.Show();
            Pump();
            Check(window.IsVisible, "Operator window opens");
            CheckLogo(window);
            Check(!Control<Button>(window, "DrawButton").IsEnabled, "Draw disabled without round");
            Control<TextBox>(window, "FirstTicketBox").Text = "ugyldig";
            try
            {
                Click(window, "StartHeartsButton");
                Check(engine.State.ActiveRound is null, "Invalid form does not start round");
            }
            finally { Control<TextBox>(window, "FirstTicketBox").Text = "73"; }
            Click(window, "StartHeartsButton");
            Check(engine.State.ActiveRound?.Mode == LotteryMode.Hearts, "Heart round starts");
            Check(!Control<StackPanel>(window, "LotterySetup").IsEnabled, "Lottery setup locked");
            Check(app.Windows.OfType<AudienceWindow>().Count() == 1, "Separate audience window opens");
            var audience = app.Windows.OfType<AudienceWindow>().Single();
            CheckLogo(audience);
            var earlyReveal = Stopwatch.StartNew();
            Click(window, "DrawButton");
            var selected = engine.State.ActiveRound!.PendingDraw
                ?? throw new InvalidOperationException("Winner must be selected before animation completes");
            Check(store.Load()!.ActiveRound!.PendingDraw!.Id == selected.Id, "Selection persisted before animation");
            Check(!Control<Button>(window, "DrawButton").IsEnabled, "Draw disabled during animation");
            Check(Control<ListBox>(audience, "PublicWinners").Items.Count == 0, "Pending winner not yet in public history");
            approve = false;
            Click(window, "EndRoundButton");
            Check(engine.State.ActiveRound.PendingDraw?.Id == selected.Id, "Declining End preserves running draw");
            approve = true;
            Click(window, "RevealButton");
            WaitUntil(() => engine.State.ActiveRound?.PendingDraw is null, "Immediate reveal completes");
            Check(earlyReveal.Elapsed.TotalSeconds < 5, "Reveal now bypasses ten-second animation");
            Check(engine.State.ActiveRound.Draws[0].Id == selected.Id, "Reveal does not reroll");
            Check(engine.State.ActiveRound!.Draws[0].Prize == "Sjokolade", "Reverse prize order");
            Check(Control<ListView>(window, "WinnersList").Items.Count == 1, "Winner list updates");
            Control<TextBox>(window, "HeartDurationBox").Text = "0,5";
            var naturalDraw = Stopwatch.StartNew();
            DrawAndWait(window, engine);
            Check(naturalDraw.Elapsed.TotalSeconds >= 0.45, "Configured heart duration is used");
            Check(engine.State.ActiveRound!.Draws[1].Number == 2, "Unique heart numbers");
            Check(audience.DisplayedNumber == "2", "Final result stays visible");
            var completedRound = engine.State.ActiveRound.Id;
            Click(window, "StartHeartsButton");
            Check(engine.State.ActiveRound!.Id != completedRound, "New heart round replaces completed round without a prompt");
            Check(engine.State.ActiveRound.Draws.Count == 0, "New heart round clears winners");
            Check(Control<ListBox>(app.Windows.OfType<AudienceWindow>().Single(), "PublicWinners").Items.Count == 0,
                "New heart round clears public history");
            DrawAndWait(window, engine);
            Check(engine.State.ActiveRound!.Draws[0].Number == 1, "New heart round resets numbers");
            DrawAndWait(window, engine);
            Click(window, "EndRoundButton");
            Check(!app.Windows.OfType<AudienceWindow>().Any(), "End round closes audience");
            Control<TabControl>(window, "SetupTabs").SelectedIndex = 1;
            Pump();
            Check(Control<CheckBox>(window, "BookAnimationBox").IsVisible, "Book animation option is in display and sound tab");
            Control<CheckBox>(window, "BookAnimationBox").IsChecked = true;
            Control<TabControl>(window, "SetupTabs").SelectedIndex = 0;
            Click(window, "StartBooksButton");
            Check(store.Load()!.Settings.BookAnimationEnabled, "Book animation option persists");
            audience = app.Windows.OfType<AudienceWindow>().Single();
            var bookDraw = Stopwatch.StartNew();
            Click(window, "DrawButton");
            Check(audience.DisplayedStage == "FØRST FINNER VI BOKA", "Book is animated first");
            WaitUntil(() => Control<TextBlock>(audience, "BookText").Text == "Bok 2 · Lodd 51–100", "Book and range revealed before ticket");
            Check(audience.DisplayedNumber == "02", "Book pause displays correct book");
            var virtualBook = Control<BookletView>(audience, "VirtualBook");
            Check(virtualBook.Visibility == Visibility.Collapsed && Control<Viewbox>(audience, "ReelsView").IsVisible,
                "Book pause retains number reels without revealing the booklet");
            Check(engine.State.ActiveRound!.PendingDraw is not null, "Book reveal is not a complete ticket reveal");
            WaitUntil(() => audience.DisplayedStage == "LODDNUMMER", "Ticket follows book pause");
            WaitUntil(() =>
            {
                if (audience.DisplayedStage != "LODDNUMMER") return true;
                Check(virtualBook.Visibility == Visibility.Collapsed && Control<Viewbox>(audience, "ReelsView").IsVisible,
                    "Booklet remains hidden throughout ticket selection");
                return false;
            }, "Ticket selection completes before booklet animation");
            Check(audience.DisplayedNumber == "73" && virtualBook.Visibility == Visibility.Collapsed &&
                Control<Viewbox>(audience, "ReelsView").IsVisible, "Final ticket remains on the reels before the front cover");
            WaitUntil(() => virtualBook.Visibility == Visibility.Visible, "Front cover follows the completed number draw");
            Check(virtualBook.Page == 0, "Front cover is shown before opening the booklet");
            WaitUntil(() => virtualBook.Page == 1, "Cover opens to the first table page");
            Check(virtualBook.HighlightedNumber is null, "Pages turn before highlighting the winning row");
            WaitUntil(() => engine.State.ActiveRound!.PendingDraw is null, "Book draw finishes");
            Check(bookDraw.Elapsed.TotalSeconds >= 1.3, "Book stages include configured pause");
            Check(audience.DisplayedNumber == "73", "Global ticket shown, not local row");
            Check(Control<TextBlock>(window, "CurrentBook").Text == "Bok 2", "Book displayed with global ticket");
            Check(virtualBook.Page == 3 && virtualBook.HighlightedNumber == 73,
                "Virtual book opens to third table page and highlights ticket 73");
            if (args.Length > 0)
            {
                Directory.CreateDirectory(args[0]);
                SaveScreenshot(window, Path.Combine(args[0], "operator.png"));
                SaveScreenshot(audience, Path.Combine(args[0], "audience.png"));
                var previousWidth = window.Width;
                var previousHeight = window.Height;
                window.Width = window.MinWidth;
                window.Height = window.MinHeight;
                Pump();
                SaveScreenshot(window, Path.Combine(args[0], "operator-small.png"));
                window.Width = previousWidth;
                window.Height = previousHeight;
            }
            Click(window, "EndRoundButton");
            window.Close();

            var restarted = new LotteryEngine(store, _ => 0);
            restarted.StartRound(LotteryMode.Books);
            var pending = restarted.SelectWinner();
            var recoverySelections = 0;
            var recovered = new LotteryEngine(store, _ => { recoverySelections++; return 0; });
            window = new MainWindow(recovered, store, (_, _) => approve);
            window.Show();
            Pump();
            Check(Control<Button>(window, "DrawButton").Content.ToString() == "Fortsett trekning", "Recovery offered");
            Check(Control<CheckBox>(window, "BookAnimationBox").IsChecked == true, "Book animation restored after restart");
            Click(window, "DrawButton");
            audience = app.Windows.OfType<AudienceWindow>().Single();
            WaitUntil(() => Control<BookletView>(audience, "VirtualBook").Visibility == Visibility.Visible,
                "Recovered draw reaches its post-draw booklet presentation");
            audience.Close();
            WaitUntil(() => Control<Button>(window, "DrawButton").Content.ToString() == "Fortsett trekning",
                "Closing audience during booklet presentation pauses animation");
            Check(store.Load()!.ActiveRound!.PendingDraw!.Id == pending.Id, "Closing audience keeps pending winner");
            Click(window, "DrawButton");
            Click(window, "RevealButton");
            WaitUntil(() => recovered.State.ActiveRound?.PendingDraw is null, "Book reveal-now skips both stages");
            Check(recovered.State.ActiveRound!.Draws.Last().Id == pending.Id, "Same pending winner recovered");
            Check(recovered.State.ActiveRound.PendingDraw is null, "Recovery reveal persisted");
            Check(Control<BookletView>(app.Windows.OfType<AudienceWindow>().Single(), "VirtualBook").HighlightedNumber == pending.Number,
                "Reveal-now after recovery highlights the saved ticket row");
            Check(recoverySelections == 0, "Recovery never requests another random selection");
            Click(window, "EndRoundButton");
            Check(Directory.GetFiles(store.LogDirectory, "*.txt").Length == 4, "One TXT log per round");
            Control<TextBox>(window, "HeartDurationBox").Text = "10";
            Click(window, "StartHeartsButton");
            Click(window, "DrawButton");
            var endedDraw = recovered.State.ActiveRound!.PendingDraw!;
            Click(window, "EndRoundButton");
            WaitUntil(() => !Control<Button>(window, "DrawButton").IsEnabled, "End stops current animation");
            Check(recovered.State.ActiveRound is null, "Early End completes round");
            Check(recovered.State.Rounds.Last().Draws.Single().Id == endedDraw.Id, "Early End retains selected winner");
            Check(!app.Windows.OfType<AudienceWindow>().Any(), "Early End closes audience");
            WaitUntil(() => Control<Button>(window, "StartHeartsButton").IsEnabled, "Cancellation finishes before another round");
            Click(window, "StartHeartsButton");
            Click(window, "DrawButton");
            var interrupted = recovered.State.ActiveRound!.PendingDraw!.Id;
            window.Close();
            Pump();
            Check(store.Load()!.ActiveRound!.PendingDraw!.Id == interrupted, "Closing operator preserves interrupted selection");
            Check(new LotteryEngine(store).SelectWinner().Id == interrupted, "Restart after operator close does not reroll");
            CheckAnimationSpeed();
            CheckBookTicketAnimation();
            CheckBookletRows();
            CheckBookletLayout();
            CheckAudio();
            Console.WriteLine("PASS: animation stages/timing, reveal-now, safe cancellation/end/restart, rounds, logs and audio.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            foreach (var window in app.Windows.Cast<Window>().ToArray()) window.Close();
            app.Shutdown();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static T Control<T>(FrameworkElement window, string name) where T : FrameworkElement =>
        (T)window.FindName(name);

    private static void Click(FrameworkElement window, string name)
    {
        var button = Control<Button>(window, name);
        Check(button.IsEnabled, name + " is enabled");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump();
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
        Dispatcher.PushFrame(frame);
    }

    private static void DrawAndWait(MainWindow window, LotteryEngine engine)
    {
        var before = engine.State.ActiveRound!.Draws.Count;
        Click(window, "DrawButton");
        Check(engine.State.ActiveRound.Draws.Count == before + 1, "Draw adds exactly one selection");
        WaitUntil(() => engine.State.ActiveRound?.PendingDraw is null, "Draw completes");
    }

    private static void WaitUntil(Func<bool> condition, string message)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed.TotalSeconds > 8) throw new TimeoutException(message);
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(10) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
            Dispatcher.PushFrame(frame);
            timer.Stop();
        }
    }

    private static void SaveScreenshot(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var dpi = VisualTreeHelper.GetDpi(element);
        var image = new RenderTargetBitmap((int)(element.ActualWidth * dpi.DpiScaleX),
            (int)(element.ActualHeight * dpi.DpiScaleY), 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
        image.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var output = File.Create(path);
        encoder.Save(output);
    }

    private static void CheckLogo(FrameworkElement window)
    {
        var image = Control<Image>(window, "BrandLogo");
        Check(image.Source is BitmapSource { PixelWidth: > 0, PixelHeight: > 0 },
            "Supplied logo loads from embedded app resources");
        Check(image.Stretch == Stretch.Uniform && image.Opacity == 1,
            "Logo preserves proportions and remains opaque");
    }

    private static void CheckDisplayPlacement()
    {
        var display = DisplayManager.GetDisplays().First();
        var audience = new AudienceWindow();
        audience.Show();
        try
        {
            DisplayManager.Place(audience, display.Device, [display]);
            Pump();
            Check(audience.WindowState == WindowState.Normal, "Single-screen audience is not maximized");
            Check(audience.WindowStyle == WindowStyle.SingleBorderWindow &&
                audience.ResizeMode == ResizeMode.CanResize && !audience.Topmost,
                "Single-screen audience has movable, resizable window chrome");
            Check(audience.Width < SystemParameters.WorkArea.Width &&
                audience.Height < SystemParameters.WorkArea.Height, "Single-screen audience does not fill screen");
            audience.Width = 640;
            audience.Height = 400;
            audience.Left += 10;
            var left = audience.Left;
            DisplayManager.Place(audience, display.Device, [display]);
            Check(audience.Width == 640 && audience.Height == 400 && audience.Left == left,
                "Subsequent draws preserve operator-adjusted audience bounds");
            var projector = display with { Device = "test-projector", Primary = false };
            DisplayManager.Place(audience, projector.Device, [display, projector]);
            Check(audience.WindowStyle == WindowStyle.None && audience.ResizeMode == ResizeMode.NoResize &&
                audience.Topmost, "Multiple screens retain borderless projector behavior");
            DisplayManager.Place(audience, "disconnected-projector", [display]);
            Check(audience.WindowState == WindowState.Normal &&
                audience.WindowStyle == WindowStyle.SingleBorderWindow && !audience.Topmost,
                "Disconnecting projector restores windowed mode");
        }
        finally { audience.Close(); }
    }

    private static void CheckAudio()
    {
        foreach (var win in new[] { false, true })
        {
            var quiet = DrawSound.CreateWave(25, win);
            var loud = DrawSound.CreateWave(75, win);
            var silent = DrawSound.CreateWave(0, win);
            Check(System.Text.Encoding.ASCII.GetString(loud, 0, 4) == "RIFF", "Audio is a local WAV");
            Check(BitConverter.ToInt32(loud, 40) == loud.Length - 44, "WAV payload length is correct");
            double quietEnergy = 0, loudEnergy = 0;
            for (var i = 44; i < loud.Length; i += 2)
            {
                quietEnergy += Math.Abs((int)BitConverter.ToInt16(quiet, i));
                loudEnergy += Math.Abs((int)BitConverter.ToInt16(loud, i));
                Check(BitConverter.ToInt16(silent, i) == 0, "Zero volume is silent");
            }
            Check(quietEnergy > 0 && loudEnergy / quietEnergy is > 2.9 and < 3.1, "Volume scales synthesized audio");
        }
        using var sound = new DrawSound();
        sound.Tick(1, false);
        sound.Stop();
        sound.Win(1, false);
        sound.Stop();
        sound.Tick(100, true);
    }

    private static void CheckAnimationSpeed()
    {
        var audience = new AudienceWindow();
        using var sound = new DrawSound();
        var round = new LotteryRound { Mode = LotteryMode.Hearts, Prizes = ["Gevinst"], Books = new() };
        var draw = new DrawRecord { Number = 73, Prize = "Gevinst" };
        var fastTicks = 0;
        var slowTicks = 0;
        var fast = audience.PlayAsync(round, draw, new() { DurationSeconds = 0.6, TickMilliseconds = 30 },
            sound, () => { fastTicks++; return (0, true); }, CancellationToken.None);
        WaitUntil(() => fast.IsCompleted, "Fast animation completes");
        fast.GetAwaiter().GetResult();
        var slow = audience.PlayAsync(round, draw, new() { DurationSeconds = 0.6, TickMilliseconds = 500 },
            sound, () => { slowTicks++; return (0, true); }, CancellationToken.None);
        WaitUntil(() => slow.IsCompleted, "Slow animation completes");
        slow.GetAwaiter().GetResult();
        Check(fastTicks > slowTicks, "Configured speed changes reel movement frequency independently of duration");
        Check(audience.DisplayedNumber == "73", "Animation speed cannot change selected winner");
        audience.Close();
    }

    private static void CheckBookletRows()
    {
        var booklet = new BookletView();
        foreach (var number in new[] { 1, 10, 11, 20, 21, 30, 31, 40, 41, 50, 51, 73, 4000 })
        {
            var draw = new DrawRecord { Number = number, Prize = "Gevinst" };
            booklet.ShowCover(draw, 80);
            Check(booklet.Page == 0 && booklet.HighlightedNumber is null, "Cover resets prior highlighted row");
            booklet.Reveal(draw);
            Check(booklet.Page == (number - 1) % 50 / 10 + 1 && booklet.HighlightedNumber == number,
                "Page and row mapping handles all table boundaries and book 80");
        }
    }

    private static void CheckBookletLayout(string? screenshotDirectory = null)
    {
        var booklet = new BookletView();
        var draw = new DrawRecord { Number = 4000, Prize = "Gevinst" };
        booklet.ShowCover(draw, 80);
        var size = new Size(booklet.Width, booklet.Height);
        booklet.Measure(size);
        booklet.Arrange(new Rect(size));
        booklet.UpdateLayout();
        var cover = booklet.Children.OfType<Border>().Single(b =>
            b.Child is Grid grid && grid.Children.OfType<Image>().Any());
        Check(Math.Abs(cover.ActualHeight / cover.ActualWidth - 210.0 / 148) < 0.01,
            "Cover has portrait A5 proportions");
        booklet.Reveal(draw);
        booklet.Measure(size);
        booklet.Arrange(new Rect(size));
        booklet.UpdateLayout();
        var paper = booklet.Children.OfType<Border>().Single(b => b != cover);
        var spread = (Grid)paper.Child;
        Check(spread.ColumnDefinitions.Count == 2, "Open book has two pages");
        foreach (var column in spread.ColumnDefinitions)
        {
            Check(column.ActualWidth <= cover.ActualWidth &&
                cover.ActualWidth - column.ActualWidth <= 2, "Each open page matches the cover width");
            Check(Math.Abs(paper.ActualHeight / column.ActualWidth - 210.0 / 148) < 0.02,
                "Open pages retain portrait A5 proportions");
        }
        var table = spread.Children.OfType<StackPanel>().Single(p => Grid.GetColumn(p) == 1);
        Check(table.ActualWidth < cover.ActualWidth && table.DesiredSize.Height <= paper.ActualHeight,
            "Number table fits within one portrait page");
        var rows = table.Children.OfType<StackPanel>().Single();
        foreach (var row in rows.Children.Cast<Grid>().Prepend(table.Children.OfType<Grid>().Single()))
        {
            foreach (var cell in row.Children.Cast<Border>())
            {
                var text = (TextBlock)cell.Child;
                var naturalText = new FormattedText(text.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch),
                    text.FontSize, text.Foreground, 1);
                Check(naturalText.Width + text.Padding.Left + text.Padding.Right <= cell.ActualWidth,
                    $"Table text '{text.Text}' fits without clipping ({naturalText.Width + text.Padding.Left + text.Padding.Right:F1} <= {cell.ActualWidth:F1})");
            }
        }
        if (screenshotDirectory is not null)
        {
            Directory.CreateDirectory(screenshotDirectory);
            SaveScreenshot(booklet, Path.Combine(screenshotDirectory, "booklet-open.png"));
        }
        Check(Application.Current.Windows.Cast<Window>().All(w => !w.IsVisible), "Layout checks never show windows");
    }

    private static void CheckBookTicketAnimation()
    {
        foreach (var number in new[] { 1, 50, 51, 100, 101, 125 })
        {
            var audience = new AudienceWindow();
            using var sound = new DrawSound();
            var draw = new DrawRecord { Number = number, Prize = "Gevinst" };
            var book = draw.Book;
            var first = (book - 1) * 50 + 1;
            var last = book * 50;
            var excluded = number == first ? first + 1 : first;
            var previous = number == last ? last - 1 : Math.Min(last, 125);
            var round = new LotteryRound
            {
                Mode = LotteryMode.Books,
                Prizes = ["Tidligere", "Gevinst"],
                Books = new() { LastTicket = 125, UnsoldTickets = excluded.ToString() },
                Draws =
                [
                    new() { Number = previous, Prize = "Tidligere", RevealedAt = DateTimeOffset.Now },
                    draw
                ]
            };
            var frames = 0;
            var animation = audience.PlayAsync(round, draw,
                new() { DurationSeconds = 0.5, TickMilliseconds = 30, BookPauseSeconds = 0 },
                sound, () =>
                {
                    if (audience.DisplayedStage == "LODDNUMMER")
                    {
                        var shown = int.Parse(audience.DisplayedNumber);
                        Check(shown >= first && shown <= last && shown <= 125, "Animated tickets stay inside the drawn book and event range");
                        Check(shown != excluded && shown != previous, "Animated tickets exclude unsold tickets and earlier winners");
                        Check(Control<TextBlock>(audience, "BookText").Text == $"Bok {book} · Lodd {first}–{last}",
                            "Book range remains visible during ticket animation");
                        frames++;
                    }
                    return (0, true);
                }, CancellationToken.None);
            WaitUntil(() => animation.IsCompleted, "Constrained book animation completes");
            animation.GetAwaiter().GetResult();
            Check(frames > 1, "Constrained ticket frames were checked");
            Check(audience.DisplayedNumber == number.ToString(), "Animation ends on the saved winner");
            Check(Control<BookletView>(audience, "VirtualBook").Visibility == Visibility.Collapsed,
                "Disabled option retains number-only animation");
            draw.RevealedAt = DateTimeOffset.Now;
            audience.UpdateRound(round);
            Check(Control<TextBlock>(audience, "BookText").Text == $"Bok {book} · Lodd {first}–{last}",
                "Range remains beside the book after reveal and refresh");
            audience.Close();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
