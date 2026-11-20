using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Julemesselotteri.Core;

namespace Julemesselotteri.App;

public partial class MainWindow : Window
{
    private readonly LotteryEngine _engine;
    private readonly EventFileStore _store;
    private readonly Func<string, string, bool>? _confirm;
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(800) };
    private readonly DrawSound _sound = new();
    private AudienceWindow? _audience;
    private CancellationTokenSource? _animationCancellation;
    private bool _drawing;
    private bool _closing;
    private bool _loading = true;

    public MainWindow(LotteryEngine engine, EventFileStore store, Func<string, string, bool>? confirm = null)
    {
        _engine = engine;
        _store = store;
        _confirm = confirm;
        InitializeComponent();
        PopulateSettings();
        _loading = false;
        _saveTimer.Tick += (_, _) => SaveSettings();
        SyncLogs();
        Render();
        if (_engine.State.ActiveRound?.PendingDraw is not null)
            SettingsMessage.Text = "En trekning ble avbrutt. Fortsett trekningen eller velg Vis nå. Samme vinner er lagret.";
    }

    private void PopulateSettings()
    {
        var s = _engine.State.Settings;
        HeartPrizesBox.Text = s.HeartPrizes;
        BookPrizesBox.Text = s.BookPrizes;
        FirstTicketBox.Text = s.Books.FirstTicket.ToString();
        LastTicketBox.Text = s.Books.LastTicket.ToString();
        UnsoldBooksBox.Text = s.Books.UnsoldBooks;
        UnsoldTicketsBox.Text = s.Books.UnsoldTickets;
        HeartDurationBox.Text = s.HeartsAnimation.DurationSeconds.ToString();
        HeartSpeedBox.Text = s.HeartsAnimation.TickMilliseconds.ToString();
        BookDurationBox.Text = s.BooksAnimation.DurationSeconds.ToString();
        BookSpeedBox.Text = s.BooksAnimation.TickMilliseconds.ToString();
        BookPauseBox.Text = s.BooksAnimation.BookPauseSeconds.ToString();
        BookAnimationBox.IsChecked = s.BookAnimationEnabled;
        VolumeSlider.Value = s.Volume;
        MutedBox.IsChecked = s.Muted;
        RefreshDisplays(s.DisplayDevice);
    }

    private void RefreshDisplays(string device)
    {
        var displays = DisplayManager.AudienceDisplays(DisplayManager.GetDisplays());
        DisplayBox.ItemsSource = displays;
        DisplayBox.SelectedItem = displays.FirstOrDefault(d => d.Device == device)
            ?? displays.FirstOrDefault();
    }

    private void SettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        if (MutedBox.IsChecked == true || VolumeSlider.Value == 0) _sound.Stop();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private bool SaveSettings()
    {
        _saveTimer.Stop();
        try
        {
            var current = _engine.State.Settings;
            var next = current with
            {
                HeartPrizes = HeartPrizesBox.Text,
                BookPrizes = BookPrizesBox.Text,
                Books = new BookSettings
                {
                    FirstTicket = ParseInt(FirstTicketBox.Text, "Første lodd"),
                    LastTicket = ParseInt(LastTicketBox.Text, "Siste lodd"),
                    UnsoldBooks = UnsoldBooksBox.Text,
                    UnsoldTickets = UnsoldTicketsBox.Text
                },
                HeartsAnimation = new AnimationSettings
                {
                    DurationSeconds = ParseDouble(HeartDurationBox.Text, "Varighet for hjertelodd"),
                    TickMilliseconds = ParseInt(HeartSpeedBox.Text, "Hastighet for hjertelodd")
                },
                BooksAnimation = new AnimationSettings
                {
                    DurationSeconds = ParseDouble(BookDurationBox.Text, "Varighet for boklodd"),
                    TickMilliseconds = ParseInt(BookSpeedBox.Text, "Hastighet for boklodd"),
                    BookPauseSeconds = ParseDouble(BookPauseBox.Text, "Bokpause")
                },
                Volume = (int)VolumeSlider.Value,
                Muted = MutedBox.IsChecked == true,
                BookAnimationEnabled = BookAnimationBox.IsChecked == true,
                DisplayDevice = (DisplayBox.SelectedItem as DisplayInfo)?.Device ?? ""
            };
            if (next != current) _engine.SaveSettings(next);
            SettingsMessage.Text = "Oppsett og trekninger er lagret på denne PC-en.";
            Render();
            return true;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            SettingsMessage.Text = $"Ikke lagret: {ex.Message}";
            return false;
        }
    }

    private static int ParseInt(string value, string name) =>
        int.TryParse(value, out var result) ? result : throw new ArgumentException($"{name}: skriv et helt tall.");

    private static double ParseDouble(string value, string name)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var result) ||
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            return result;
        throw new ArgumentException($"{name}: skriv et tall.");
    }

    private static bool IsExpected(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException
            or System.ComponentModel.Win32Exception;

    private bool Confirm(string message, string title) => _confirm?.Invoke(message, title) ??
        MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private void Execute(Action action)
    {
        try
        {
            action();
            SyncLogs();
            Render();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            SettingsMessage.Text = $"Handlingen ble stoppet: {ex.Message}";
            Render();
        }
    }

    private bool SyncLogs()
    {
        try
        {
            _store.WriteLogs(_engine.State);
            return true;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            SettingsMessage.Text = $"Trekningen er lagret, men TXT-loggen kunne ikke skrives: {ex.Message}";
            return false;
        }
    }

    private void StartHearts_Click(object sender, RoutedEventArgs e) => StartRound(LotteryMode.Hearts);
    private void StartBooks_Click(object sender, RoutedEventArgs e) => StartRound(LotteryMode.Books);

    private void StartRound(LotteryMode mode)
    {
        if (_drawing) return;
        if (_engine.State.ActiveRound is { } active)
        {
            if ((active.PendingDraw is not null || active.Draws.Count < active.Prizes.Count) &&
                !Confirm("Avslutte den uferdige runden og starte en ny? Gevinster som ikke er trukket, hoppes over.", "Ny runde"))
                return;
            Execute(() =>
            {
                _engine.RevealWinner();
                _engine.EndRound();
                CloseAudience();
            });
            if (_engine.State.ActiveRound is not null) return;
        }
        if (!SaveSettings()) return;
        Execute(() =>
        {
            _engine.StartRound(mode);
            EnsureAudience();
        });
    }

    private async void Draw_Click(object sender, RoutedEventArgs e)
    {
        if (_drawing || !SaveSettings()) return;
        try
        {
            var draw = _engine.SelectWinner();
            SyncLogs();
            EnsureAudience();
            var round = _engine.State.ActiveRound!;
            var settings = _engine.State.Settings;
            using var cancellation = new CancellationTokenSource();
            _animationCancellation = cancellation;
            _drawing = true;
            Render();
            await _audience!.PlayAsync(round, draw, round.Mode == LotteryMode.Hearts
                ? settings.HeartsAnimation : settings.BooksAnimation, _sound, SoundOptions, cancellation.Token,
                settings.BookAnimationEnabled);
            cancellation.Token.ThrowIfCancellationRequested();
            _engine.RevealWinner();
            SyncLogs();
            var options = SoundOptions();
            _sound.Win(options.Volume, options.Muted);
        }
        catch (OperationCanceledException)
        {
            _sound.Stop();
            if (!_closing && _engine.State.ActiveRound?.PendingDraw is not null)
                SettingsMessage.Text = "Trekningen er satt på pause. Fortsett trekningen med samme lagrede vinner.";
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            _sound.Stop();
            SettingsMessage.Text = $"Trekningen ble stoppet: {ex.Message}";
        }
        finally
        {
            _animationCancellation = null;
            _drawing = false;
            if (!_closing) Render();
        }
    }

    private (int Volume, bool Muted) SoundOptions() => ((int)VolumeSlider.Value, MutedBox.IsChecked == true);

    private async void Reveal_Click(object sender, RoutedEventArgs e)
    {
        if (_drawing)
        {
            _audience?.RevealNow();
            return;
        }
        AudienceWindow? audience = null;
        Execute(() =>
        {
            _engine.RevealWinner();
            EnsureAudience();
            audience = _audience;
        });
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
        if (audience is null || audience != _audience || _drawing) return;
        var options = SoundOptions();
        _sound.Win(options.Volume, options.Muted);
    }

    private void EndRound_Click(object sender, RoutedEventArgs e)
    {
        var round = _engine.State.ActiveRound;
        if (round is null) return;
        if ((round.Draws.Count < round.Prizes.Count || round.PendingDraw is not null) &&
            !Confirm("Avslutte runden før alle gevinstene er vist? Et allerede valgt nummer beholdes som vinner.", "Avslutt runde"))
            return;
        Execute(() =>
        {
            _animationCancellation?.Cancel();
            _engine.RevealWinner();
            _engine.EndRound();
            CloseAudience();
        });
    }

    private void ShowAudience_Click(object sender, RoutedEventArgs e) => Execute(EnsureAudience);

    private void EnsureAudience()
    {
        if (_engine.State.ActiveRound is null) return;
        var displays = DisplayManager.GetDisplays();
        DisplayManager.PlaceOperator(this, displays);
        var opening = _audience is null || !_audience.IsVisible;
        if (_audience is null)
        {
            _audience = new AudienceWindow();
            _audience.Closed += (_, _) =>
            {
                _audience = null;
                _animationCancellation?.Cancel();
                _sound.Stop();
            };
        }
        DisplayManager.Place(_audience, _engine.State.Settings.DisplayDevice, displays);
        if (opening)
        {
            _audience.Show();
            Activate();
        }
        _audience.UpdateRound(_engine.State.ActiveRound, _engine.State.Settings.BookAnimationEnabled);
    }

    private void CloseAudience()
    {
        _audience?.Close();
        _audience = null;
    }

    private void Render()
    {
        var state = _engine.State;
        var round = state.ActiveRound;
        LotterySetup.IsEnabled = round is null;
        StartHeartsButton.IsEnabled = !_drawing && (round is null || round.Mode == LotteryMode.Hearts);
        StartBooksButton.IsEnabled = !_drawing && round is null;
        NewEventButton.IsEnabled = !_drawing && round is null;
        EndRoundButton.IsEnabled = round is not null;
        DrawButton.IsEnabled = !_drawing && round is not null && (round.PendingDraw is not null || round.Draws.Count < round.Prizes.Count);
        DrawButton.Content = _drawing ? "Trekker …" : round?.PendingDraw is not null ? "Fortsett trekning" : "Trekk neste gevinst";
        RevealButton.IsEnabled = round?.PendingDraw is not null;
        ShowAudienceButton.IsEnabled = round is not null;
        RoundTitle.Text = round is null ? "Klar for trekning" : round.Mode == LotteryMode.Hearts ? "Hjertelodd" : "Boklodd";
        RoundStatus.Text = round is null ? "Velg lotteri for å starte en runde."
            : $"{round.Draws.Count(d => d.RevealedAt is not null)} av {round.Prizes.Count} gevinster vist";
        var latest = round?.Draws.LastOrDefault(d => d.RevealedAt is not null);
        CurrentPrize.Text = round?.PendingDraw?.Prize ?? latest?.Prize ?? round?.Prizes.FirstOrDefault() ?? "Ingen aktiv runde";
        CurrentNumber.Text = _drawing ? "Trekker …" : round?.PendingDraw is not null ? "Lagret" : latest?.Number.ToString() ?? "—";
        CurrentBook.Text = round?.PendingDraw is null && latest is not null && round?.Mode == LotteryMode.Books ? $"Bok {latest.Book}" : "";
        WinnersList.ItemsSource = round?.Draws.Where(d => d.RevealedAt is not null).Reverse()
            .Select(d => new WinnerItem(d.Prize, d.Number.ToString())).ToArray();
        EligibilityText.Text = $"Boklodd: {_engine.EligibleCount(LotteryMode.Books)} ledige nummer etter utelatelser og tidligere vinnere.";
        _audience?.UpdateRound(round, state.Settings.BookAnimationEnabled);
    }

    private void NewEvent_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveSettings() || !SyncLogs()) return;
        if (!Confirm("Starte et helt nytt arrangement? Alle nummer blir tilgjengelige igjen. Eksisterende TXT-logger beholdes.", "Nytt arrangement"))
            return;
        Execute(_engine.NewEvent);
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e) => Execute(() =>
    {
        Directory.CreateDirectory(_store.LogDirectory);
        Process.Start(new ProcessStartInfo { FileName = _store.LogDirectory, UseShellExecute = true });
    });

    private void RefreshDisplays_Click(object sender, RoutedEventArgs e) => Execute(() =>
    {
        RefreshDisplays((DisplayBox.SelectedItem as DisplayInfo)?.Device ?? "");
        if (_audience is not null) EnsureAudience();
    });

    private void TestSound_Click(object sender, RoutedEventArgs e)
    {
        if (_drawing) return;
        var options = SoundOptions();
        _sound.Win(options.Volume, options.Muted);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!SaveSettings() && !Confirm("Oppsettet kunne ikke lagres. Lukke likevel? Tidligere lagrede trekninger er bevart.", "Oppsett ikke lagret"))
        {
            e.Cancel = true;
            return;
        }
        _closing = true;
        _saveTimer.Stop();
        _animationCancellation?.Cancel();
        SyncLogs();
        CloseAudience();
        _sound.Dispose();
    }
}

public sealed record WinnerItem(string Prize, string NumberText);
