using System.Security.Cryptography;

namespace Julemesselotteri.Core;

public sealed class LotteryEngine
{
    private readonly IEventStore _store;
    private readonly Func<int, int> _randomIndex;
    private EventState _state;

    public LotteryEngine(IEventStore store, Func<int, int>? randomIndex = null)
    {
        _store = store;
        _randomIndex = randomIndex ?? RandomNumberGenerator.GetInt32;
        _state = store.Load() ?? EventState.Create();
        _store.Save(_state);
    }

    public EventState State => EventFileStore.Copy(_state);

    public void SaveSettings(AppSettings settings)
    {
        TicketRules.ValidateSettings(settings);
        Change(state => state.Settings = settings);
    }

    public int EligibleCount(LotteryMode mode) => Eligible(_state, mode, _state.Settings.Books).Length;

    private static int[] Eligible(EventState state, LotteryMode mode, BookSettings books)
    {
        if (mode == LotteryMode.Books)
            return TicketRules.EligibleBooks(books, state.Rounds.Where(r => r.Mode == mode)
                .SelectMany(r => r.Draws).Select(d => d.Number));
        var excluded = state.ActiveRound?.Draws.Select(d => d.Number).ToHashSet() ?? [];
        return Enumerable.Range(1, 1000).Where(n => !excluded.Contains(n)).ToArray();
    }

    public void StartRound(LotteryMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentException("Ukjent lotteri.");
        if (_state.ActiveRound is not null)
            throw new InvalidOperationException("Avslutt den aktive runden først.");
        var prizes = TicketRules.PrizeOrder(mode == LotteryMode.Hearts
            ? _state.Settings.HeartPrizes : _state.Settings.BookPrizes);
        if (prizes.Count == 0)
            throw new InvalidOperationException("Legg inn minst én gevinst.");
        if (prizes.Count > Eligible(_state, mode, _state.Settings.Books).Length)
            throw new InvalidOperationException("Det finnes færre ledige lodd enn gevinster.");
        Change(state => state.Rounds.Add(new LotteryRound
        {
            Mode = mode,
            Prizes = prizes,
            Books = state.Settings.Books
        }));
    }

    public DrawRecord SelectWinner()
    {
        var round = _state.ActiveRound ?? throw new InvalidOperationException("Start en runde først.");
        if (round.PendingDraw is not null)
            return EventFileStore.Copy(_state).ActiveRound!.PendingDraw!;
        var extra = round.Draws.Count == round.Prizes.Count;
        if (extra && round.Mode != LotteryMode.Hearts)
            throw new InvalidOperationException("Alle gevinstene er trukket. Avslutt runden.");
        var eligible = Eligible(_state, round.Mode, round.Books);
        if (eligible.Length == 0)
            throw new InvalidOperationException("Ingen ledige lodd.");
        var draw = new DrawRecord
        {
            Number = eligible[_randomIndex(eligible.Length)],
            Prize = extra ? $"EKSTRAPREMIE {round.ExtraPrizeCount + 1}" : round.Prizes[round.Draws.Count]
        };
        Change(state =>
        {
            var active = state.ActiveRound!;
            if (extra)
            {
                active.Prizes.Add(draw.Prize);
                active.ExtraPrizeCount++;
            }
            active.Draws.Add(draw);
        });
        return State.ActiveRound!.Draws.Last();
    }

    public void RevealWinner()
    {
        if (_state.ActiveRound?.PendingDraw is null)
            return;
        Change(state => state.ActiveRound!.PendingDraw!.RevealedAt = DateTimeOffset.Now);
    }

    public void EndRound()
    {
        if (_state.ActiveRound is null)
            return;
        if (_state.ActiveRound.PendingDraw is not null)
            throw new InvalidOperationException("Vis det valgte vinnernummeret før runden avsluttes.");
        Change(state => state.ActiveRound!.EndedAt = DateTimeOffset.Now);
    }

    public void NewEvent()
    {
        if (_state.ActiveRound is not null)
            throw new InvalidOperationException("Avslutt den aktive runden først.");
        var state = EventState.Create(_state.Settings);
        _store.Save(state);
        _state = state;
    }

    private void Change(Action<EventState> apply)
    {
        var next = EventFileStore.Copy(_state);
        apply(next);
        _store.Save(next);
        _state = next;
    }
}
