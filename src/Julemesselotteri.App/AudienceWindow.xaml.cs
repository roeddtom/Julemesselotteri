using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Julemesselotteri.Core;

namespace Julemesselotteri.App;

public partial class AudienceWindow : Window
{
    private TaskCompletionSource _revealSignal = new();
    public bool IsAnimating { get; private set; }
    public string DisplayedNumber { get; private set; } = "";
    public string DisplayedStage => StageText.Text;

    public AudienceWindow() => InitializeComponent();

    public void UpdateRound(LotteryRound? round)
    {
        if (round is null) return;
        ModeText.Text = round.Mode == LotteryMode.Hearts ? "Hjertelodd" : "Boklodd";
        var winners = round.Draws.Where(d => d.RevealedAt is not null).Reverse()
            .Select(d => new WinnerItem(d.Prize, round.Mode == LotteryMode.Books
                ? $"{d.Number} · Bok {d.Book}" : d.Number.ToString())).ToArray();
        PublicWinners.ItemsSource = winners;
        if (winners.Length > 0) PublicWinners.ScrollIntoView(winners[0]);
        WinnerCountText.Text = $"{winners.Length} av {round.Prizes.Count} gevinster";
        if (IsAnimating) return;
        var last = round.Draws.LastOrDefault(d => d.RevealedAt is not null);
        PrizeText.Text = round.PendingDraw?.Prize ?? last?.Prize ?? round.Prizes.First();
        StageText.Text = round.PendingDraw is not null ? "TREKNING VENTER" : last is null ? "ER DERE KLARE?" : "VINNERNUMMER";
        ShowDigits(round.PendingDraw is not null ? "—" : last?.Number.ToString() ?? "—", false);
        BookText.Text = round.PendingDraw is null && last is not null && round.Mode == LotteryMode.Books ? $"Bok {last.Book}" : "";
        HintText.Text = last is null || round.PendingDraw is not null ? "Lykke til!" : "Gratulerer!";
    }

    public void RevealNow() => _revealSignal.TrySetResult();

    public async Task PlayAsync(LotteryRound round, DrawRecord draw, AnimationSettings settings,
        DrawSound sound, Func<(int Volume, bool Muted)> soundOptions, CancellationToken token)
    {
        if (IsAnimating) throw new InvalidOperationException("En trekning pågår allerede.");
        _revealSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IsAnimating = true;
        try
        {
            PrizeText.Text = draw.Prize;
            BookText.Text = "";
            HintText.Text = "Hvem blir den heldige?";
            if (round.Mode == LotteryMode.Books)
            {
                StageText.Text = "FØRST FINNER VI BOKA";
                await SpinAsync(draw.Book, Math.Max(2, ((round.Books.LastTicket - 1) / 50 + 1).ToString().Length),
                    settings, sound, soundOptions, token);
                BookText.Text = $"Bok {draw.Book}";
                HintText.Text = "Finn fram boka …";
                await WaitAsync(TimeSpan.FromSeconds(settings.BookPauseSeconds), token);
            }
            StageText.Text = "LODDNUMMER";
            HintText.Text = "Lykke til!";
            await SpinAsync(draw.Number, Math.Max(4,
                    (round.Mode == LotteryMode.Hearts ? 1000 : round.Books.LastTicket).ToString().Length),
                settings, sound, soundOptions, token);
            StageText.Text = "VINNERNUMMER";
            ShowDigits(draw.Number.ToString(), false);
            HintText.Text = "Gratulerer!";
        }
        finally
        {
            IsAnimating = false;
        }
    }

    private async Task SpinAsync(int result, int digits, AnimationSettings settings, DrawSound sound,
        Func<(int Volume, bool Muted)> soundOptions, CancellationToken token)
    {
        var final = result.ToString().PadLeft(digits, '0');
        var watch = Stopwatch.StartNew();
        var duration = TimeSpan.FromSeconds(settings.DurationSeconds);
        while (watch.Elapsed < duration && !_revealSignal.Task.IsCompleted)
        {
            token.ThrowIfCancellationRequested();
            var progress = watch.Elapsed.TotalSeconds / duration.TotalSeconds;
            var display = new char[digits];
            for (var i = 0; i < digits; i++)
                display[i] = progress > 0.65 + 0.25 * i / digits
                    ? final[i] : (char)('0' + Random.Shared.Next(10));
            var interval = settings.TickMilliseconds * (1 + 3 * Math.Pow(progress, 3));
            ShowDigits(new string(display), true, interval);
            var options = soundOptions();
            sound.Tick(options.Volume, options.Muted);
            await WaitAsync(TimeSpan.FromMilliseconds(Math.Max(1,
                Math.Min(interval, (duration - watch.Elapsed).TotalMilliseconds))), token);
        }
        token.ThrowIfCancellationRequested();
        ShowDigits(final, false);
    }

    private async Task WaitAsync(TimeSpan delay, CancellationToken token)
    {
        using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        await Task.WhenAny(Task.Delay(delay, delayCancellation.Token), _revealSignal.Task);
        delayCancellation.Cancel();
        token.ThrowIfCancellationRequested();
    }

    private void ShowDigits(string value, bool animate, double interval = 100)
    {
        DisplayedNumber = value;
        if (ReelsPanel.Children.Count != value.Length)
        {
            ReelsPanel.Children.Clear();
            foreach (var _ in value) ReelsPanel.Children.Add(new DigitReel());
        }
        for (var i = 0; i < value.Length; i++)
            ((DigitReel)ReelsPanel.Children[i]).SetDigit(value[i], animate, interval);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
