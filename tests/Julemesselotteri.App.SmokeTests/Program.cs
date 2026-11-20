using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
            if (args.FirstOrDefault() == "--headless-displays")
            {
                CheckDisplayRouting();
                CheckOperatorPlacement();
                CheckDisplayPlacement();
                Console.WriteLine("PASS: primary operator placement, separate audience routing and single-screen fallback; no windows shown.");
                return 0;
            }
            if (args.FirstOrDefault() == "--headless-booklet")
            {
                CheckStableNumberPresentation();
                CheckBookletRows();
                CheckBookletLayout(args.Skip(1).FirstOrDefault());
                CheckAudienceLayout();
                CheckIdleRoundAndSpinPlan();
                CheckHeartFrame(args.Skip(1).FirstOrDefault());
                CheckBookTicketAnimation();
                CheckAnimationSpeed();
                CheckBookPresentationTiming();
                CheckAudio(playback: false);
                Console.WriteLine("PASS: portrait booklet layout, ticket mapping and reel animations; no windows shown.");
                return 0;
            }
            CheckDisplayPlacement();
            CheckDisplayRouting();
            CheckOperatorPlacement();
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
            Check(naturalDraw.Elapsed.TotalSeconds >= 2.2, "Heart duration includes the three-reel braking phase");
            Check(engine.State.ActiveRound!.Draws[1].Number == 2, "Unique heart numbers");
            Check(audience.DisplayedNumber == "002", "Final result stays visible without removing reels");
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
            Check(Control<CheckBox>(window, "BookAnimationBox").IsChecked == false, "Book animation starts disabled");
            Control<CheckBox>(window, "BookAnimationBox").IsChecked = true;
            Control<TabControl>(window, "SetupTabs").SelectedIndex = 0;
            Click(window, "StartBooksButton");
            Check(store.Load()!.Settings.BookAnimationEnabled, "Book animation option persists");
            audience = app.Windows.OfType<AudienceWindow>().Single();
            var bookDraw = Stopwatch.StartNew();
            Click(window, "DrawButton");
            Check(audience.DisplayedStage == "LODDNUMMER", "Book lottery draws the ticket directly");
            Check(Control<TextBlock>(audience, "BookText").Text == "", "Book is not revealed before the ticket");
            var virtualBook = Control<BookletView>(audience, "VirtualBook");
            Check(virtualBook.Visibility == Visibility.Collapsed && Control<Viewbox>(audience, "ReelsView").IsVisible,
                "Ticket drawing shows the reels without revealing the booklet");
            Check(engine.State.ActiveRound!.PendingDraw is not null, "Ticket remains pending during animation");
            WaitUntil(() =>
            {
                if (audience.DisplayedStage != "LODDNUMMER") return true;
                Check(virtualBook.Visibility == Visibility.Collapsed && Control<Viewbox>(audience, "ReelsView").IsVisible,
                    "Booklet remains hidden throughout ticket selection");
                return false;
            }, "Ticket selection completes before booklet animation");
            Check(audience.DisplayedNumber == "0073" && virtualBook.Visibility == Visibility.Collapsed &&
                Control<Viewbox>(audience, "ReelsView").IsVisible, "Final ticket remains on the reels before the front cover");
            Check(Control<TextBlock>(audience, "BookText").Text == "Bok 2", "Book label omits its ticket range");
            WaitUntil(() => virtualBook.Visibility == Visibility.Visible, "Front cover follows the completed number draw");
            Check(virtualBook.Page == 0, "Front cover is shown before opening the booklet");
            WaitUntil(() => virtualBook.Page == 1, "Cover opens to the first table page");
            Check(virtualBook.HighlightedNumber is null, "Pages turn before highlighting the winning row");
            WaitUntil(() => engine.State.ActiveRound!.PendingDraw is null, "Book draw finishes");
            Check(bookDraw.Elapsed.TotalSeconds >= 6.4, "Draw includes staggered digit stops, booklet pause and slower pages");
            Check(audience.DisplayedNumber == "0073", "Global ticket shown with stable reel count, not local row");
            Check(Control<TextBlock>(window, "CurrentBook").Text == "Bok 2", "Book displayed with global ticket");
            Check(Control<ListView>(window, "WinnersList").Items.Cast<WinnerItem>().Single().NumberText == "73",
                "Operator winner history contains only the ticket number");
            Check(Control<ListBox>(audience, "PublicWinners").Items.Cast<WinnerItem>().Single().NumberText == "73",
                "Public winner history contains only the ticket number");
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
            WaitUntil(() => recovered.State.ActiveRound?.PendingDraw is null, "Reveal-now skips ticket reels and booklet presentation");
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
            CheckStableNumberPresentation();
            CheckBookTicketAnimation();
            CheckBookletRows();
            CheckBookletLayout();
            CheckAudienceLayout();
            CheckIdleRoundAndSpinPlan();
            CheckHeartFrame();
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

    private static void WaitUntil(Func<bool> condition, string message, double timeoutSeconds = 20)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed.TotalSeconds > timeoutSeconds) throw new TimeoutException(message);
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
            Check(WindowBounds(audience) == new Rect(projector.Left, projector.Top, projector.Width, projector.Height),
                "Audience is placed at the selected display's physical pixel bounds");
            Check(!IsWindowVisible(new WindowInteropHelper(audience).Handle), "Audience placement does not show the window");
            DisplayManager.Place(audience, "disconnected-projector", [display]);
            Check(audience.WindowState == WindowState.Normal &&
                audience.WindowStyle == WindowStyle.SingleBorderWindow && !audience.Topmost,
                "Disconnecting projector restores windowed mode");
            Check(!audience.IsVisible && !IsWindowVisible(new WindowInteropHelper(audience).Handle),
                "Single-screen fallback does not show the window");
        }
        finally { audience.Close(); }
    }

    private static void CheckDisplayRouting()
    {
        var primary = new DisplayInfo("main", "Main", true, 0, 0, 1920, 1080);
        var projector = new DisplayInfo("projector", "Projector", false, -1920, -200, 1920, 1080);
        var third = new DisplayInfo("third", "Third", false, 1920, 0, 2560, 1440);
        DisplayInfo[] displays = [projector, primary, third];
        Check(DisplayManager.SelectAudienceDisplay(primary.Device, displays) == projector,
            "Saved primary display cannot place the audience over the operator");
        Check(DisplayManager.SelectAudienceDisplay(third.Device, displays) == third,
            "Explicit secondary display choice is preserved");
        Check(DisplayManager.SelectAudienceDisplay("disconnected", displays) == projector &&
            DisplayManager.SelectAudienceDisplay("", displays) == projector,
            "Missing or disconnected preference falls back to a non-primary display");
        Check(DisplayManager.AudienceDisplays(displays).SequenceEqual([projector, third]),
            "Audience selector excludes the main display when alternatives exist");
        Check(DisplayManager.SelectAudienceDisplay(projector.Device, [primary]) == primary,
            "One display remains available for a separate window");
        Check(DisplayManager.AudienceDisplays([primary]).Single() == primary,
            "Single-screen selector retains the main display");
    }

    private static void CheckOperatorPlacement()
    {
        var displays = DisplayManager.GetDisplays();
        var primary = displays.First(d => d.Primary);
        var window = new Window { Width = 800, Height = 500, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        try
        {
            DisplayManager.PlaceOperator(window, displays.OrderBy(d => d.Primary).ToArray());
            Check(window.WindowStartupLocation == WindowStartupLocation.Manual,
                "Default startup placement cannot override the primary monitor");
            Check(primary.WorkArea.Contains(WindowBounds(window)), "Operator fits inside the primary monitor's work area");
            Check(!window.IsVisible && !IsWindowVisible(new WindowInteropHelper(window).Handle),
                "Operator can be positioned without displaying it");
        }
        finally { window.Close(); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint handle, out NativeRect bounds);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint handle);

    private static Rect WindowBounds(Window window)
    {
        Check(GetWindowRect(new WindowInteropHelper(window).EnsureHandle(), out var bounds), "Native window bounds can be read");
        return new Rect(bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
    }

    private static void CheckAudio(bool playback = true)
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
            Check(quietEnergy > 0 && loudEnergy / quietEnergy is > 2.9 and < 3.1, "Volume scales recorded audio");
        }
        if (!playback) return;
        using var sound = new DrawSound();
        sound.Spin(1, false);
        sound.Stop();
        sound.Win(1, false);
        sound.Stop();
        sound.Spin(100, true);
    }

    private static void CheckAnimationSpeed()
    {
        var audience = new AudienceWindow();
        using var sound = new DrawSound();
        var round = new LotteryRound { Mode = LotteryMode.Hearts, Prizes = ["Gevinst"], Books = new() };
        var draw = new DrawRecord { Number = 73, Prize = "Gevinst" };
        Check(CountChanges(30) > CountChanges(500),
            "Configured speed changes reel movement frequency independently of duration");
        Check(audience.DisplayedNumber == "073", "Animation speed cannot change selected winner");
        audience.Close();

        int CountChanges(int tickMilliseconds)
        {
            var previous = "";
            var changes = 0;
            var checkedAudio = false;
            var animation = audience.PlayAsync(round, draw, new() { DurationSeconds = 0.6, TickMilliseconds = tickMilliseconds },
                sound, () =>
                {
                    checkedAudio = true;
                    if (previous != audience.DisplayedNumber) changes++;
                    previous = audience.DisplayedNumber;
                    return (0, true);
                }, CancellationToken.None);
            Check(!checkedAudio && audience.DisplayedNumber.Length == 3, "All heart reels are populated before the spin sound");
            WaitUntil(() => animation.IsCompleted, "Animation completes");
            animation.GetAwaiter().GetResult();
            return changes;
        }
    }

    private static void CheckIdleRoundAndSpinPlan()
    {
        var audience = new AudienceWindow();
        using var sound = new DrawSound();
        try
        {
            foreach (var mode in new[] { LotteryMode.Hearts, LotteryMode.Books })
            {
                var round = new LotteryRound { Mode = mode, Books = new(), Prizes = ["Skjult gevinst"] };
                audience.UpdateRound(round, true);
                Check(Control<Viewbox>(audience, "PrizeView").Visibility == Visibility.Collapsed &&
                    Control<TextBlock>(audience, "PrizeText").Text == "", "New round does not expose the first prize");
                Check(Control<Viewbox>(audience, "ReelsView").Visibility == Visibility.Collapsed &&
                    audience.DisplayedNumber == "", "New round does not show empty number reels");
                var draw = new DrawRecord { Number = 73, Prize = round.Prizes[0] };
                var animation = audience.PlayAsync(round, draw,
                    new() { DurationSeconds = 10, TickMilliseconds = 30, BookPauseSeconds = 0 },
                    sound, () => (0, true), CancellationToken.None);
                Check(Control<Viewbox>(audience, "PrizeView").Visibility == Visibility.Visible &&
                    Control<TextBlock>(audience, "PrizeText").Text == draw.Prize &&
                    Control<Viewbox>(audience, "ReelsView").Visibility == Visibility.Visible,
                    "Prize and reels appear only when drawing starts");
                Check(Control<System.Windows.Shapes.Path>(audience, "HeartFrame").Visibility ==
                    (mode == LotteryMode.Hearts ? Visibility.Visible : Visibility.Collapsed),
                    "Only heart lottery draws frame the slot numbers with a heart");
                audience.RevealNow();
                WaitUntil(() => animation.IsCompleted, "Reveal-now bypasses all independent reels");
                animation.GetAwaiter().GetResult();
                Check(audience.DisplayedNumber == (mode == LotteryMode.Hearts ? "073" : "0073"),
                    "Reveal-now still shows the saved winner without resizing");
            }
        }
        finally { audience.Close(); }

        foreach (var result in new[] { 0, 3, 9 })
        {
            for (var position = 0; position < 4; position++)
            {
                const double spin = 2;
                var rate = 28 + position * 2;
                var brake = AudienceWindow.BrakeDuration(position);
                var finish = spin + brake;
                double At(double time) => AudienceWindow.SpinPosition(result, time, spin, brake, rate);
                Check(Math.Abs(At(0.1) - At(0) - rate * 0.1) < 0.0001,
                    "Each reel rolls immediately at its own rate");
                Check(At(spin + 0.1) - At(spin) < At(spin) - At(spin - 0.1),
                    "Every reel starts braking at the configured spin time");
                Check(result - At(finish - 0.02) is > 0 and < 0.01 && At(finish) == result && At(finish + 1) == result,
                    "Each reel eases into the saved digit and stays there");
                if (position > 0)
                    Check(At(spin + AudienceWindow.BrakeDuration(position - 1)) < result,
                        "A reel is still moving when the more significant reel locks");
            }
        }
    }

    private static void CheckHeartFrame(string? screenshotDirectory = null)
    {
        var audience = new AudienceWindow();
        try
        {
            using var sound = new DrawSound();
            var draw = new DrawRecord { Number = 1000, Prize = "Hovedgevinst" };
            var round = new LotteryRound { Mode = LotteryMode.Hearts, Books = new(), Prizes = [draw.Prize], Draws = [draw] };
            var panel = Control<StackPanel>(audience, "ReelsPanel");
            var animation = audience.PlayAsync(round, draw, new() { DurationSeconds = 0.5, TickMilliseconds = 30 },
                sound, () =>
                {
                    Check(panel.Children.Count == 3, "Even a draw of 1000 spins only three heart reels");
                    return (0, true);
                }, CancellationToken.None);
            var spinningReels = panel.Children.Cast<DigitReel>().ToArray();
            Check(spinningReels.Length == 3, "A heart draw starts with three reels");
            WaitUntil(() => animation.IsCompleted, "The 1000 draw completes");
            animation.GetAwaiter().GetResult();
            Check(audience.DisplayedNumber == "1000" && panel.Children.Count == 4 &&
                panel.Children.Cast<DigitReel>().Skip(1).SequenceEqual(spinningReels),
                "The thousands reel appears only at the reveal, keeping the three original reels");
            draw.RevealedAt = DateTimeOffset.Now;
            audience.UpdateRound(round);
            var content = (FrameworkElement)audience.Content;
            content.Measure(new Size(1600, 900));
            content.Arrange(new Rect(0, 0, 1600, 900));
            content.UpdateLayout();
            var heart = Control<System.Windows.Shapes.Path>(audience, "HeartFrame");
            var slots = Control<Viewbox>(audience, "SlotsView");
            Check(heart.Visibility == Visibility.Visible && audience.DisplayedNumber == "1000",
                "The heart remains around the completed winning number");
            foreach (var corner in new[] { new Point(), new Point(slots.ActualWidth, 0),
                new Point(0, slots.ActualHeight), new Point(slots.ActualWidth, slots.ActualHeight) })
            {
                Check(heart.RenderedGeometry.FillContains(slots.TranslatePoint(corner, heart)),
                    "All four slot digits fit inside the heart outline");
            }
            var topLeft = heart.TranslatePoint(new Point(), content);
            var bottomRight = heart.TranslatePoint(new Point(heart.ActualWidth, heart.ActualHeight), content);
            Check(topLeft.X >= 0 && topLeft.Y >= 0 && bottomRight.X <= content.ActualWidth &&
                bottomRight.Y <= content.ActualHeight, "Heart fits within the public display");
            if (screenshotDirectory is not null)
            {
                Directory.CreateDirectory(screenshotDirectory);
                SaveScreenshot(content, Path.Combine(screenshotDirectory, "audience-hearts.png"));
            }
            audience.UpdateRound(new LotteryRound { Mode = LotteryMode.Books, Books = new(), Prizes = [draw.Prize], Draws = [draw] });
            Check(heart.Visibility == Visibility.Collapsed, "Book lottery does not retain the heart decoration");
            Check(!audience.IsVisible, "Heart layout checks never display the window");
        }
        finally { audience.Close(); }
    }

    private static void CheckBookletRows()
    {
        var booklet = new BookletView();
        foreach (var number in new[] { 1, 10, 11, 20, 21, 30, 31, 40, 41, 50, 51, 73, 4000 })
        {
            var draw = new DrawRecord { Number = number, Prize = "Gevinst" };
            booklet.ShowCover(draw);
            Check(booklet.Page == 0 && booklet.HighlightedNumber is null, "Cover resets prior highlighted row");
            booklet.Reveal(draw);
            Check(booklet.Page == (number - 1) % 50 / 10 + 1 && booklet.HighlightedNumber == number,
                "Page and row mapping handles all table boundaries and book 80");
        }
    }

    private static void CheckBookletLayout(string? screenshotDirectory = null)
    {
        var visibleWindows = Application.Current.Windows.Cast<Window>().Where(window => window.IsVisible).ToArray();
        var booklet = new BookletView();
        var draw = new DrawRecord { Number = 4000, Prize = "Gevinst" };
        booklet.ShowCover(draw);
        var size = new Size(booklet.Width, booklet.Height);
        booklet.Measure(size);
        booklet.Arrange(new Rect(size));
        booklet.UpdateLayout();
        var cover = booklet.Children.OfType<Border>().Single(b =>
            b.Child is Grid grid && grid.Children.OfType<Image>().Any());
        Check(((Grid)cover.Child).Children.OfType<TextBlock>().Single().Text == "Bok 80",
            "Book cover shows its number without a range or total");
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
        var summary = spread.Children.OfType<StackPanel>().Single(p => Grid.GetColumn(p) == 0);
        Check(summary.Children.Count == 1 && summary.Children[0] is Viewbox { Child: TextBlock { Text: "Vinner\n4000" } },
            "Left page contains only the winner label and ticket number");
        var winnerView = (Viewbox)summary.Children[0];
        var winner = (TextBlock)winnerView.Child;
        var transform = winner.TransformToAncestor(spread);
        var origin = transform.Transform(new Point());
        var scaledFont = transform.Transform(new Point(0, winner.FontSize)).Y - origin.Y;
        Check(scaledFont >= 55, "Winner label remains substantially larger after fitting a four-digit result");
        Check(winner.Inlines.LastInline is System.Windows.Documents.Run { FontSize: 104 },
            "Winning number is emphasized beyond the enlarged winner label");
        Check(winnerView.ActualWidth <= spread.ColumnDefinitions[0].ActualWidth &&
            winnerView.ActualHeight <= spread.ActualHeight, "Large winner text fits within the portrait page");
        var tableTop = table.TranslatePoint(new Point(), spread).Y;
        Check(Math.Abs(tableTop - (spread.ActualHeight - table.ActualHeight) / 2) < 1,
            "Table is vertically centered on the right page");
        Check(table.Children[0] is Grid && !table.Children.OfType<TextBlock>().Any(),
            "Table page starts with column headings without a repeated book or ticket-range label");
        Check(table.ActualWidth < cover.ActualWidth && table.DesiredSize.Height <= paper.ActualHeight,
            "Number table fits within one portrait page");
        var rows = table.Children.OfType<StackPanel>().Single();
        foreach (var row in rows.Children.Cast<Grid>().Prepend(table.Children.OfType<Grid>().Single()))
        {
            foreach (var cell in row.Children.OfType<Border>())
            {
                var text = (TextBlock)cell.Child;
                var naturalText = new FormattedText(text.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch),
                    text.FontSize, text.Foreground, 1);
                Check(naturalText.Width + text.Padding.Left + text.Padding.Right <= cell.ActualWidth,
                    $"Table text '{text.Text}' fits without clipping ({naturalText.Width + text.Padding.Left + text.Padding.Right:F1} <= {cell.ActualWidth:F1})");
            }
        }
        var handwriting = rows.Children.Cast<Grid>().Select(row => row.Children.OfType<Image>().Single()).ToArray();
        foreach (var image in handwriting)
        {
            Check(image.Source is BitmapSource { PixelWidth: > 0, PixelHeight: > 0 } &&
                image.Stretch == Stretch.Uniform, "Every ticket row contains a loaded, proportionally scaled handwriting image");
            Check(Grid.GetColumn(image) == 1 && Grid.GetColumnSpan(image) == 2,
                "Handwriting spans name and phone columns without covering ticket numbers");
            Check(image.ActualWidth <= table.ActualWidth - 50 && image.ActualHeight <= 25,
                "Handwriting remains within its row and the name/phone area");
        }
        booklet.ShowPage(1, false);
        booklet.Reveal(draw);
        Check(rows.Children.Cast<Grid>().Select(row => row.Children.OfType<Image>().Single().Source)
            .SequenceEqual(handwriting.Select(image => image.Source)),
            "Returning to a page preserves the same randomly assigned writing for each ticket");
        for (var index = 1; index <= 40; index++)
        {
            var source = new BitmapImage(new Uri(
                $"pack://application:,,,/Julemesselotteri.App;component/Assets/book-rows/rad_{index:00}.png"));
            Check(source.PixelWidth > 0 && source.PixelHeight > 0, "All forty supplied row images are embedded");
        }
        if (screenshotDirectory is not null)
        {
            Directory.CreateDirectory(screenshotDirectory);
            SaveScreenshot(booklet, Path.Combine(screenshotDirectory, "booklet-open.png"));
        }
        Check(Application.Current.Windows.Cast<Window>().All(window => !window.IsVisible || visibleWindows.Contains(window)),
            "Layout checks do not show additional windows");
    }

    private static void CheckAudienceLayout()
    {
        var audience = new AudienceWindow();
        try
        {
            var round = new LotteryRound
            {
                Mode = LotteryMode.Hearts,
                Books = new(),
                Prizes = Enumerable.Range(1, 30).Select(n => $"Gevinst {n}").ToList(),
                Draws = Enumerable.Range(1, 30).Select(n => new DrawRecord
                {
                    Number = n,
                    Prize = $"Gevinst {n}",
                    RevealedAt = DateTimeOffset.Now
                }).ToList()
            };
            audience.UpdateRound(round);
            var content = (FrameworkElement)audience.Content;
            content.Measure(new Size(1600, 900));
            content.Arrange(new Rect(0, 0, 1600, 900));
            content.UpdateLayout();
            var winners = Control<ListBox>(audience, "PublicWinners");
            var scroller = Descendants(winners).OfType<ScrollViewer>().Single();
            Check(scroller.ComputedVerticalScrollBarVisibility == Visibility.Collapsed &&
                scroller.ComputedHorizontalScrollBarVisibility == Visibility.Collapsed,
                "Public winner list hides scrollbars even when the list overflows");
            Check(scroller.ScrollableHeight > 0, "Hidden scrollbar preserves programmatic scrolling");
            scroller.ScrollToBottom();
            content.UpdateLayout();
            Check(scroller.VerticalOffset > 0, "Overflowing winner history can scroll without a visible scrollbar");
            audience.UpdateRound(round);
            content.UpdateLayout();
            Check(scroller.VerticalOffset == 0, "Public winner list still scrolls automatically to the newest winner");
            Check(!Descendants(content).OfType<TextBlock>().Any(text =>
                text.Text.Contains("Små lodd", StringComparison.OrdinalIgnoreCase)),
                "Audience layout no longer shows the bottom slogan");
            Check(!Descendants(content).OfType<TextBlock>().Any(text =>
                text.Text.Contains("Takk for at du støtter", StringComparison.OrdinalIgnoreCase)),
                "The winner list has no thank-you footer");
            Check(audience.FindName("HintText") is null, "The good-luck footer is removed for all draw stages");
            Check(!Descendants(content).OfType<TextBlock>().Any(text =>
                text.Text is "JULEMESSELOTTERI" or "Hjertelodd" or "Boklodd" or "NÅ TREKKER VI"),
                "Extra audience headings are removed");
            Check(!audience.IsVisible, "Audience layout checks never display the window");
        }
        finally { audience.Close(); }

        static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                yield return child;
                foreach (var descendant in Descendants(child)) yield return descendant;
            }
        }
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
            var previous = number == Math.Min(last, 125) ? number - 1 : Math.Min(last, 125);
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
            var ticketWatch = new Stopwatch();
            var rightChangesAfterLeftStops = 0;
            var previousDigits = "";
            var animation = audience.PlayAsync(round, draw,
                new() { DurationSeconds = 0.5, TickMilliseconds = 30, BookPauseSeconds = 0 },
                sound, () =>
                {
                    if (audience.DisplayedStage == "LODDNUMMER")
                    {
                        if (!ticketWatch.IsRunning) ticketWatch.Start();
                        var shown = audience.DisplayedNumber;
                        Check(shown.Length == 4 && shown.All(char.IsAsciiDigit), "All four reels display decimal digits");
                        if (ticketWatch.Elapsed.TotalSeconds > 0.5 + AudienceWindow.BrakeDuration(0))
                        {
                            Check(shown[0] == number.ToString("D4")[0], "Leftmost reel stays stopped while the others spin");
                            if (previousDigits.Length == 4 && shown[3] != previousDigits[3])
                                rightChangesAfterLeftStops++;
                        }
                        previousDigits = shown;
                        Check(Control<TextBlock>(audience, "BookText").Text == "",
                            "Book number remains hidden until all ticket digits stop");
                        frames++;
                    }
                    return (0, true);
                }, CancellationToken.None);
            Check(audience.DisplayedStage == "LODDNUMMER", "Book lottery skips the separate book-number drawing");
            WaitUntil(() => animation.IsCompleted, "Independent ticket reels complete");
            animation.GetAwaiter().GetResult();
            Check(frames > 1 && rightChangesAfterLeftStops > 1, "Rightmost digit continues spinning after the leftmost stops");
            Check(ticketWatch.Elapsed.TotalSeconds >= 2.5, "Ticket reels retain the staggered braking phase");
            Check(audience.DisplayedNumber == number.ToString("D4"), "Animation ends on the saved winner without resizing");
            Check(Control<TextBlock>(audience, "BookText").Text == $"Bok {book}",
                "Book number without range appears beneath the completed result");
            Check(Control<BookletView>(audience, "VirtualBook").Visibility == Visibility.Collapsed,
                "Disabled option retains number-only animation");
            draw.RevealedAt = DateTimeOffset.Now;
            audience.UpdateRound(round);
            Check(Control<TextBlock>(audience, "BookText").Text == $"Bok {book}",
                "Book label without range remains after reveal and refresh");
            audience.Close();
        }
    }

    private static void CheckBookPresentationTiming()
    {
        var audience = new AudienceWindow();
        using var sound = new DrawSound();
        try
        {
            var draw = new DrawRecord { Number = 1, Prize = "Gevinst" };
            var round = new LotteryRound { Mode = LotteryMode.Books, Books = new(), Prizes = [draw.Prize], Draws = [draw] };
            var settings = new AnimationSettings { DurationSeconds = 0.5, TickMilliseconds = 30, BookPauseSeconds = 0.4 };
            var animation = audience.PlayAsync(round, draw, settings, sound, () => (0, true), CancellationToken.None, true);
            var before = AudienceBounds(audience);
            Check(audience.DisplayedStage == "LODDNUMMER", "Ticket reels start directly without book drawing or pause");
            WaitUntil(() => audience.DisplayedStage == "VINNERNUMMER", "Ticket number settles");
            var pause = Stopwatch.StartNew();
            var booklet = Control<BookletView>(audience, "VirtualBook");
            WaitUntil(() => booklet.Visibility == Visibility.Visible, "Booklet appears after configured pause");
            Check(pause.Elapsed.TotalSeconds >= 0.35, "Configured pause still precedes optional booklet");
            Check(AudienceBounds(audience).SequenceEqual(before), "Booklet cover does not replace or move the reels");
            pause.Restart();
            WaitUntil(() => booklet.Page == 1, "Slower cover opens");
            Check(pause.Elapsed.TotalSeconds >= 1.55, "Cover is displayed for twice the previous duration");
            pause.Restart();
            WaitUntil(() => animation.IsCompleted, "Slower page turn completes");
            animation.GetAwaiter().GetResult();
            Check(pause.Elapsed.TotalSeconds >= 0.55 && booklet.HighlightedNumber == 1,
                "Page turn lasts twice as long before revealing the saved winner");
            draw.RevealedAt = DateTimeOffset.Now;
            audience.UpdateRound(round, true);
            Check(AudienceBounds(audience).SequenceEqual(before), "Open booklet and winner refresh preserve the draw layout");

            round = new LotteryRound { Mode = LotteryMode.Hearts, Books = new(), Prizes = [draw.Prize], Draws = [draw] };
            animation = audience.PlayAsync(round, draw, settings, sound, () => (0, true), CancellationToken.None);
            pause.Restart();
            WaitUntil(() => pause.Elapsed.TotalSeconds >= 1.45, "Leftmost digit stops before revealing remaining reels");
            Check(!animation.IsCompleted, "Right-hand reels are still spinning");
            pause.Restart();
            audience.RevealNow();
            WaitUntil(() => animation.IsCompleted, "Reveal-now stops all remaining reels");
            animation.GetAwaiter().GetResult();
            Check(pause.Elapsed.TotalSeconds < 0.5 && audience.DisplayedNumber == "001",
                "Reveal-now bypasses remaining braking without changing the saved result");

            using var cancellation = new CancellationTokenSource();
            animation = audience.PlayAsync(round, draw, settings, sound, () => (0, true), cancellation.Token, true);
            cancellation.Cancel();
            WaitUntil(() => animation.IsCompleted, "Cancellation stops every reel");
            Check(animation.IsCanceled && !audience.IsAnimating, "Canceled reels do not continue the draw");
        }
        finally { audience.Close(); }
    }

    private static void CheckStableNumberPresentation()
    {
        var audience = new AudienceWindow();
        using var sound = new DrawSound();
        try
        {
            foreach (var mode in new[] { LotteryMode.Books, LotteryMode.Hearts })
            {
                var draw = new DrawRecord { Number = 73, Prize = "Gevinst" };
                var round = new LotteryRound { Mode = mode, Books = new(), Prizes = [draw.Prize], Draws = [draw] };
                var animation = audience.PlayAsync(round, draw,
                    new() { DurationSeconds = 0.5, TickMilliseconds = 30 },
                    sound, () => (0, true), CancellationToken.None);
                var panel = Control<StackPanel>(audience, "ReelsPanel");
                var reels = panel.Children.Cast<DigitReel>().ToArray();
                var before = AudienceBounds(audience);
                if (mode == LotteryMode.Hearts) audience.RevealNow();
                WaitUntil(() => animation.IsCompleted, "Ticket reels settle");
                animation.GetAwaiter().GetResult();
                Check(AudienceBounds(audience).SequenceEqual(before), $"{mode} result keeps its layout after stopping");
                Check(panel.Children.Cast<DigitReel>().SequenceEqual(reels), "Completion does not rebuild the reels");
                Check(audience.DisplayedNumber == (mode == LotteryMode.Hearts ? "073" : "0073"),
                    "Leading zeroes remain so the result does not resize");
                Check(Control<TextBlock>(audience, "BookText").Text == (mode == LotteryMode.Books ? "Bok 2" : ""),
                    "Only book draws show the book number below the result");
                draw.RevealedAt = DateTimeOffset.Now;
                audience.UpdateRound(round);
                Check(AudienceBounds(audience).SequenceEqual(before) && panel.Children.Cast<DigitReel>().SequenceEqual(reels),
                    $"Saving and refreshing the revealed {mode} winner preserves the layout");
                Check(Control<ListBox>(audience, "PublicWinners").Items.Cast<WinnerItem>().Single().NumberText == "73",
                    "Public winner history contains only the ticket number");
            }
        }
        finally { audience.Close(); }
    }

    private static Rect[] AudienceBounds(AudienceWindow audience)
    {
        var content = (FrameworkElement)audience.Content;
        content.Measure(new Size(1600, 900));
        content.Arrange(new Rect(0, 0, 1600, 900));
        content.UpdateLayout();
        return new[] { "PrizeView", "StageText", "ReelsPanel", "PublicWinners" }
            .Select(name => Control<FrameworkElement>(audience, name))
            .Select(element => element.TransformToAncestor(content).TransformBounds(new Rect(element.RenderSize)))
            .ToArray();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
