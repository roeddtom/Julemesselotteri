using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Julemesselotteri.Core;

namespace Julemesselotteri.App;

public partial class AudienceWindow : Window
{
    private const int HeartSpinDigits = 3;
    private TaskCompletionSource _revealSignal = new();
    public bool IsAnimating { get; private set; }
    public string DisplayedNumber { get; private set; } = "";
    public string DisplayedStage => StageText.Text;

    public AudienceWindow() => InitializeComponent();

    public void UpdateRound(LotteryRound? round, bool bookAnimationEnabled = false)
    {
        if (round is null) return;
        var winners = round.Draws.Where(d => d.RevealedAt is not null).Reverse()
            .Select(d => new WinnerItem(d.Prize, d.Number.ToString())).ToArray();
        PublicWinners.ItemsSource = winners;
        if (winners.Length > 0) PublicWinners.ScrollIntoView(winners[0]);
        WinnerCountText.Text = $"{winners.Length} av {round.Prizes.Count} gevinster";
        if (IsAnimating) return;
        var useBooklet = SetPresentationMode(round.Mode, bookAnimationEnabled);
        var last = round.Draws.LastOrDefault(d => d.RevealedAt is not null);
        var showResult = round.PendingDraw is null && last is not null;
        ReelsView.Visibility = showResult ? Visibility.Visible : Visibility.Collapsed;
        HeartFrame.Visibility = showResult && round.Mode == LotteryMode.Hearts ? Visibility.Visible : Visibility.Collapsed;
        if (useBooklet && showResult) ShowBookletResult(last!);
        else HideBooklet(useBooklet);
        PrizeText.Text = round.PendingDraw?.Prize ?? last?.Prize ?? "";
        PrizeView.Visibility = PrizeText.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        StageText.Text = round.PendingDraw is not null ? "TREKNING VENTER" : last is null ? "ER DERE KLARE?" : DrawLabel(round.Mode);
        ShowDigits(showResult ? FormatNumber(last!.Number, round) : "");
        BookText.Text = showResult && round.Mode == LotteryMode.Books ? $"Bok {last!.Book}" : "";
    }

    public void RevealNow() => _revealSignal.TrySetResult();

    public async Task PlayAsync(LotteryRound round, DrawRecord draw, AnimationSettings settings,
        DrawSound sound, Func<(int Volume, bool Muted)> soundOptions, CancellationToken token,
        bool bookAnimationEnabled = false)
    {
        if (IsAnimating) throw new InvalidOperationException("En trekning pågår allerede.");
        _revealSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IsAnimating = true;
        try
        {
            PrizeText.Text = draw.Prize;
            PrizeView.Visibility = Visibility.Visible;
            var useBooklet = SetPresentationMode(round.Mode, bookAnimationEnabled);
            HideBooklet(useBooklet);
            ReelsView.Visibility = Visibility.Visible;
            HeartFrame.Visibility = round.Mode == LotteryMode.Hearts ? Visibility.Visible : Visibility.Collapsed;
            BookText.Text = "";
            StageText.Text = DrawLabel(round.Mode);
            var final = FormatNumber(draw.Number, round);
            var spinning = round.Mode == LotteryMode.Hearts ? final[^HeartSpinDigits..] : final;
            await SpinAsync(spinning, settings, sound, soundOptions, token);
            ShowDigits(final);
            BookText.Text = round.Mode == LotteryMode.Books ? $"Bok {draw.Book}" : "";
            if (useBooklet)
            {
                await WaitAsync(TimeSpan.FromSeconds(settings.BookPauseSeconds), token);
                BookletSlot.Visibility = Visibility.Visible;
                VirtualBook.ShowCover(draw);
                await WaitAsync(TimeSpan.FromMilliseconds(1600), token);
                var targetPage = (draw.Number - 1) % 50 / 10 + 1;
                for (var page = 1; page <= targetPage; page++)
                {
                    VirtualBook.ShowPage(page, true);
                    await WaitAsync(TimeSpan.FromMilliseconds(600), token);
                }
                ShowBookletResult(draw);
            }
            await Dispatcher.Yield(DispatcherPriority.Background);
            token.ThrowIfCancellationRequested();
        }
        finally
        {
            IsAnimating = false;
        }
    }

    private static string FormatNumber(int number, LotteryRound round) =>
        number.ToString().PadLeft(round.Mode == LotteryMode.Hearts
            ? HeartSpinDigits
            : Math.Max(4, round.Books.LastTicket.ToString().Length), '0');

    private static string DrawLabel(LotteryMode mode) => mode == LotteryMode.Books ? "LODDNUMMER" : "";

    private bool SetPresentationMode(LotteryMode mode, bool bookAnimationEnabled)
    {
        var hearts = mode == LotteryMode.Hearts;
        PrizeArea.Width = hearts ? 1152 : 990;
        PrizeArea.Margin = hearts ? new Thickness(320, 0, 0, 0) : new Thickness(0, 120, 0, 0);
        StageText.Margin = hearts ? new Thickness(0, 35, 0, 20) : new Thickness(0, 142, 0, 20);
        BookText.Visibility = hearts ? Visibility.Collapsed : Visibility.Visible;
        return !hearts && bookAnimationEnabled;
    }

    private void HideBooklet(bool reserveSpace)
    {
        BookletSlot.Visibility = reserveSpace ? Visibility.Hidden : Visibility.Collapsed;
        VirtualBook.Visibility = Visibility.Collapsed;
    }

    private void ShowBookletResult(DrawRecord draw)
    {
        BookletSlot.Visibility = Visibility.Visible;
        VirtualBook.Reveal(draw);
    }

    private async Task SpinAsync(string target, AnimationSettings settings, DrawSound sound,
        Func<(int Volume, bool Muted)> soundOptions, CancellationToken token)
    {
        PrepareReels(target.Length);
        var reels = ReelsPanel.Children.Cast<DigitReel>().ToArray();
        var rates = reels.Select(_ => (0.88 + Random.Shared.NextDouble() * 0.24) * 1000 / settings.TickMilliseconds).ToArray();
        var finish = settings.DurationSeconds + BrakeDuration(reels.Length - 1);
        var ticks = rates.SelectMany((rate, i) => DigitCrossings(settings.DurationSeconds, BrakeDuration(i), rate));
        var track = DrawSound.MixTicks(ticks, finish);
        var displayed = new char[reels.Length];
        UpdateFrame(0);
        await Dispatcher.Yield(DispatcherPriority.Background);
        var watch = Stopwatch.StartNew();
        try
        {
            while (!_revealSignal.Task.IsCompleted)
            {
                token.ThrowIfCancellationRequested();
                var elapsed = watch.Elapsed.TotalSeconds;
                if (elapsed >= finish) break;
                UpdateFrame(elapsed);
                var options = soundOptions();
                sound.Spin(track, elapsed, options.Volume, options.Muted);
                await WaitAsync(TimeSpan.FromMilliseconds(16), token);
            }
            token.ThrowIfCancellationRequested();
        }
        finally
        {
            if (token.IsCancellationRequested || _revealSignal.Task.IsCompleted) sound.Stop();
        }

        void UpdateFrame(double elapsed)
        {
            var changed = false;
            for (var i = 0; i < reels.Length; i++)
            {
                var position = SpinPosition(target[i] - '0', elapsed, settings.DurationSeconds, BrakeDuration(i), rates[i]);
                var digit = reels[i].SetSpinPosition(position);
                changed |= digit != displayed[i];
                displayed[i] = digit;
            }
            if (changed) DisplayedNumber = new string(displayed);
        }
    }

    internal static double BrakeDuration(int position) => 0.85 + position * 0.45;

    internal static IEnumerable<double> DigitCrossings(double spinDuration, double brakeDuration, double rate)
    {
        // Invert SpinPosition at each centered digit, working forward to the lock.
        var brakingDistance = rate * brakeDuration / 2;
        var travel = rate * (spinDuration + brakeDuration / 2);
        for (var distance = (int)Math.Ceiling(travel) - 1; distance >= 0; distance--)
            yield return distance >= brakingDistance
                ? (travel - distance) / rate
                : spinDuration + brakeDuration - Math.Sqrt(2 * brakeDuration * distance / rate);
    }

    internal static double SpinPosition(int result, double elapsed, double spinDuration, double brakeDuration, double rate)
    {
        // Work back from the saved digit so the continuous motion ends exactly on it.
        if (elapsed < spinDuration) return result - rate * (spinDuration - elapsed + brakeDuration / 2);
        var remaining = Math.Max(0, spinDuration + brakeDuration - elapsed);
        return result - rate * remaining * remaining / (2 * brakeDuration);
    }

    private async Task WaitAsync(TimeSpan delay, CancellationToken token)
    {
        using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        await Task.WhenAny(Task.Delay(delay, delayCancellation.Token), _revealSignal.Task);
        delayCancellation.Cancel();
        token.ThrowIfCancellationRequested();
    }

    private void ShowDigits(string value)
    {
        DisplayedNumber = value;
        PrepareReels(value.Length);
        for (var i = 0; i < value.Length; i++)
            ((DigitReel)ReelsPanel.Children[i]).SetDigit(value[i]);
    }

    private void PrepareReels(int count)
    {
        while (ReelsPanel.Children.Count > count) ReelsPanel.Children.RemoveAt(0);
        while (ReelsPanel.Children.Count < count) ReelsPanel.Children.Insert(0, new DigitReel());
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
