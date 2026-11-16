using System.Windows;
using System.Windows.Input;
using Julemesselotteri.Core;

namespace Julemesselotteri.App;

public partial class AudienceWindow : Window
{
    public AudienceWindow() => InitializeComponent();

    public void UpdateRound(LotteryRound? round)
    {
        if (round is null) return;
        ModeText.Text = round.Mode == LotteryMode.Hearts ? "Hjertelodd" : "Boklodd";
        var last = round.Draws.LastOrDefault(d => d.RevealedAt is not null);
        PrizeText.Text = round.PendingDraw?.Prize ?? last?.Prize ?? round.Prizes.First();
        NumberText.Text = last?.Number.ToString() ?? "—";
        BookText.Text = last is not null && round.Mode == LotteryMode.Books ? $"Bok {last.Book}" : "";
        PublicWinners.ItemsSource = round.Draws.Where(d => d.RevealedAt is not null).Reverse()
            .Select(d => new WinnerItem(d.Prize, round.Mode == LotteryMode.Books
                ? $"{d.Number} · Bok {d.Book}" : d.Number.ToString())).ToArray();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
