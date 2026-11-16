using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Julemesselotteri.App;
using Julemesselotteri.Core;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
        var root = Path.Combine(Path.GetTempPath(), "Julemesselotteri-ui-" + Guid.NewGuid());
        var app = new App();
        app.InitializeComponent();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            var store = new EventFileStore(root, Path.Combine(root, "logs"));
            var engine = new LotteryEngine(store, _ => 0);
            engine.SaveSettings(new AppSettings { HeartPrizes = "Hovedgevinst\nSjokolade", BookPrizes = "Sykkel" });
            var window = new MainWindow(engine, store);
            window.Show();
            Pump();
            Check(window.IsVisible, "Operator window opens");
            Check(!Control<Button>(window, "DrawButton").IsEnabled, "Draw disabled without round");
            Control<TextBox>(window, "FirstTicketBox").Text = "ugyldig";
            Click(window, "StartHeartsButton");
            Check(engine.State.ActiveRound is null, "Invalid form does not start round");
            Control<TextBox>(window, "FirstTicketBox").Text = "1";
            Click(window, "StartHeartsButton");
            Check(engine.State.ActiveRound?.Mode == LotteryMode.Hearts, "Heart round starts");
            Check(!Control<StackPanel>(window, "LotterySetup").IsEnabled, "Lottery setup locked");
            Check(app.Windows.OfType<AudienceWindow>().Count() == 1, "Separate audience window opens");
            Click(window, "DrawButton");
            Check(engine.State.ActiveRound!.Draws[0].Prize == "Sjokolade", "Reverse prize order");
            Check(Control<ListView>(window, "WinnersList").Items.Count == 1, "Winner list updates");
            Click(window, "DrawButton");
            Check(engine.State.ActiveRound!.Draws[1].Number == 2, "Unique heart numbers");
            Click(window, "EndRoundButton");
            Check(!app.Windows.OfType<AudienceWindow>().Any(), "End round closes audience");
            Click(window, "StartHeartsButton");
            Click(window, "DrawButton");
            Check(engine.State.ActiveRound!.Draws[0].Number == 1, "New heart round resets numbers");
            Click(window, "DrawButton");
            Click(window, "EndRoundButton");
            Click(window, "StartBooksButton");
            Click(window, "DrawButton");
            Check(Control<TextBlock>(window, "CurrentBook").Text == "Bok 1", "Book displayed with global ticket");
            Click(window, "EndRoundButton");
            window.Close();

            var restarted = new LotteryEngine(store, _ => 0);
            restarted.StartRound(LotteryMode.Books);
            var pending = restarted.SelectWinner();
            var recovered = new LotteryEngine(store, _ => throw new Exception("Recovery rerolled"));
            window = new MainWindow(recovered, store);
            window.Show();
            Pump();
            Check(Control<Button>(window, "DrawButton").Content.ToString() == "Fortsett trekning", "Recovery offered");
            Click(window, "RevealButton");
            Check(recovered.State.ActiveRound!.Draws.Last().Id == pending.Id, "Same pending winner recovered");
            Check(recovered.State.ActiveRound.PendingDraw is null, "Recovery reveal persisted");
            Click(window, "EndRoundButton");
            Check(Directory.GetFiles(store.LogDirectory, "*.txt").Length == 4, "One TXT log per round");
            window.Close();
            Console.WriteLine("PASS: operator workflow, audience lifecycle, validation, logs and restart recovery.");
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

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
