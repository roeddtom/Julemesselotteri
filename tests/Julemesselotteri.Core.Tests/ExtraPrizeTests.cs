using Julemesselotteri.Core;

namespace Julemesselotteri.Core.Tests;

public sealed class ExtraPrizeTests
{
    private sealed class MemoryStore : IEventStore
    {
        public EventState? Saved;
        public bool Fail;
        public EventState? Load() => Saved;
        public void Save(EventState state)
        {
            if (Fail) throw new IOException("Disk full");
            Saved = state;
        }
    }

    [Fact]
    public void ExtrasAreNumberedSeparatelyFromConfiguredPrizesAndResetWithRound()
    {
        var store = new MemoryStore();
        var engine = new LotteryEngine(store, _ => 0);
        engine.SaveSettings(new AppSettings { HeartPrizes = "EKSTRAPREMIE 1" });
        engine.StartRound(LotteryMode.Hearts);
        Assert.Equal("EKSTRAPREMIE 1", engine.SelectWinner().Prize);
        engine.RevealWinner();

        var first = engine.SelectWinner();
        Assert.Equal(("EKSTRAPREMIE 1", 2), (first.Prize, first.Number));
        Assert.Equal(first.Id, engine.SelectWinner().Id);
        engine.RevealWinner();
        var second = engine.SelectWinner();
        Assert.Equal(("EKSTRAPREMIE 2", 3), (second.Prize, second.Number));
        engine.RevealWinner();
        engine.EndRound();
        engine.StartRound(LotteryMode.Hearts);
        engine.SelectWinner();
        engine.RevealWinner();
        Assert.Equal(("EKSTRAPREMIE 1", 2), (engine.SelectWinner().Prize, engine.State.ActiveRound!.Draws.Last().Number));
    }

    [Fact]
    public void FailedExtraSaveDoesNotAppendPrizeOrConsumeNumber()
    {
        var store = new MemoryStore();
        var engine = new LotteryEngine(store, _ => 0);
        engine.SaveSettings(new AppSettings { HeartPrizes = "Premie" });
        engine.StartRound(LotteryMode.Hearts);
        engine.SelectWinner();
        engine.RevealWinner();
        store.Fail = true;
        Assert.Throws<IOException>(() => engine.SelectWinner());
        Assert.Single(engine.State.ActiveRound!.Prizes);
        Assert.Single(engine.State.ActiveRound.Draws);
        Assert.Equal(0, engine.State.ActiveRound.ExtraPrizeCount);
        store.Fail = false;
        Assert.Equal(("EKSTRAPREMIE 1", 2), (engine.SelectWinner().Prize, engine.State.ActiveRound!.Draws.Last().Number));
    }

    [Fact]
    public void ExhaustedHeartRoundDoesNotAppendAnExtraPrize()
    {
        var round = new LotteryRound
        {
            Mode = LotteryMode.Hearts,
            Books = new(),
            Prizes = Enumerable.Repeat("Premie", 1000).ToList(),
            Draws = Enumerable.Range(1, 1000)
                .Select(number => new DrawRecord { Number = number, Prize = "Premie", RevealedAt = DateTimeOffset.Now }).ToList()
        };
        var state = EventState.Create();
        state.Rounds.Add(round);
        var engine = new LotteryEngine(new MemoryStore { Saved = state }, _ => throw new Exception("No selection expected"));
        Assert.Throws<InvalidOperationException>(() => engine.SelectWinner());
        Assert.Equal(1000, engine.State.ActiveRound!.Prizes.Count);
        Assert.Equal(0, engine.State.ActiveRound.ExtraPrizeCount);
    }

    [Fact]
    public void BookRoundDoesNotOfferExtras()
    {
        var engine = new LotteryEngine(new MemoryStore(), _ => 0);
        engine.SaveSettings(new AppSettings { BookPrizes = "Premie" });
        engine.StartRound(LotteryMode.Books);
        engine.SelectWinner();
        engine.RevealWinner();
        Assert.Throws<InvalidOperationException>(() => engine.SelectWinner());
        Assert.Single(engine.State.ActiveRound!.Prizes);
    }
}
