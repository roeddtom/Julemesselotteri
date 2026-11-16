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
            Click(window, "StartBooksButton");
            audience = app.Windows.OfType<AudienceWindow>().Single();
            var bookDraw = Stopwatch.StartNew();
            Click(window, "DrawButton");
            Check(audience.DisplayedStage == "FØRST FINNER VI BOKA", "Book is animated first");
            WaitUntil(() => Control<TextBlock>(audience, "BookText").Text == "Bok 2", "Book revealed before ticket");
            Check(audience.DisplayedNumber == "02", "Book pause displays correct book");
            Check(engine.State.ActiveRound!.PendingDraw is not null, "Book reveal is not a complete ticket reveal");
            WaitUntil(() => audience.DisplayedStage == "LODDNUMMER", "Ticket follows book pause");
            WaitUntil(() => engine.State.ActiveRound!.PendingDraw is null, "Book draw finishes");
            Check(bookDraw.Elapsed.TotalSeconds >= 1.3, "Book stages include configured pause");
            Check(audience.DisplayedNumber == "73", "Global ticket shown, not local row");
            Check(Control<TextBlock>(window, "CurrentBook").Text == "Bok 2", "Book displayed with global ticket");
            if (args.Length > 0)
            {
                Directory.CreateDirectory(args[0]);
                SaveScreenshot(window, Path.Combine(args[0], "operator.png"));
                SaveScreenshot(audience, Path.Combine(args[0], "audience.png"));
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
            Click(window, "DrawButton");
            audience = app.Windows.OfType<AudienceWindow>().Single();
            WaitUntil(() => Control<TextBlock>(audience, "BookText").Text == $"Bok {pending.Book}",
                "Recovered book reaches its pause");
            audience.Close();
            WaitUntil(() => Control<Button>(window, "DrawButton").Content.ToString() == "Fortsett trekning",
                "Closing audience pauses animation");
            Check(store.Load()!.ActiveRound!.PendingDraw!.Id == pending.Id, "Closing audience keeps pending winner");
            Click(window, "DrawButton");
            Click(window, "RevealButton");
            WaitUntil(() => recovered.State.ActiveRound?.PendingDraw is null, "Book reveal-now skips both stages");
            Check(recovered.State.ActiveRound!.Draws.Last().Id == pending.Id, "Same pending winner recovered");
            Check(recovered.State.ActiveRound.PendingDraw is null, "Recovery reveal persisted");
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

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
