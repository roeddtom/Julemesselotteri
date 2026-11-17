using Julemesselotteri.Core;
using Xunit.Abstractions;

namespace Julemesselotteri.Core.Tests;

public sealed class LotteryTests(ITestOutputHelper output)
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

    private static LotteryEngine Create(MemoryStore? store = null)
    {
        var engine = new LotteryEngine(store ?? new MemoryStore(), _ => 0);
        engine.SaveSettings(new AppSettings { HeartPrizes = "Hovedgevinst\nSjokolade", BookPrizes = "Sykkel\nKake" });
        return engine;
    }

    [Fact]
    public void CryptographicDrawsHaveNoStatisticallyExcessiveConcentrationInTenThousandSamples()
    {
        const int sampleCount = 10_000;
        const int ticketCount = 4_000;
        const int bucketSize = 100;
        var counts = new int[ticketCount];
        var engine = new LotteryEngine(new MemoryStore());
        engine.SaveSettings(new AppSettings { BookPrizes = "Premie", Books = new() { LastTicket = ticketCount } });
        for (var sample = 0; sample < sampleCount; sample++)
        {
            engine.StartRound(LotteryMode.Books);
            var number = engine.SelectWinner().Number;
            Assert.InRange(number, 1, ticketCount);
            counts[number - 1]++;
            engine.RevealWinner();
            engine.EndRound();
            // Reset eligibility so samples are independent draws with replacement, unlike real event winners.
            engine.NewEvent();
        }

        var expectedPerTicket = (double)sampleCount / ticketCount;
        var ticketChiSquared = counts.Sum(count => Math.Pow(count - expectedPerTicket, 2) / expectedPerTicket);
        var buckets = counts.Chunk(bucketSize).Select(bucket => bucket.Sum()).ToArray();
        var expectedPerBucket = expectedPerTicket * bucketSize;
        var bucketChiSquared = buckets.Sum(count => Math.Pow(count - expectedPerBucket, 2) / expectedPerBucket);
        var maximum = counts.Max();
        var histogram = counts.GroupBy(count => count).OrderBy(group => group.Key)
            .Select(group => $"{group.Key} hits: {group.Count()} numbers");
        output.WriteLine($"Samples: {sampleCount}; range: 1–{ticketCount}; expected hits per number: {expectedPerTicket:F1}");
        output.WriteLine(string.Join("; ", histogram));
        output.WriteLine($"Maximum: {maximum}; numbers above five hits: {counts.Count(count => count > 5)}");
        output.WriteLine($"Ticket Pearson statistic (3999 df): {ticketChiSquared:F2}; 40-bucket statistic (39 df): {bucketChiSquared:F2}");

        // Conservative smoke-test limits, not a proof of randomness. Rare false alarms remain possible.
        Assert.True(ticketChiSquared < 4_800, $"Excessive per-ticket imbalance: {ticketChiSquared:F2}");
        Assert.True(bucketChiSquared < 120, $"Excessive imbalance across 100-number ranges: {bucketChiSquared:F2}");
        Assert.True(maximum < 20, $"An individual ticket occurred unusually often: {maximum} hits");
    }

    [Fact]
    public void DrawsPrizesInReverseOrderAndKeepsSelectionUntilRevealed()
    {
        var engine = Create();
        engine.StartRound(LotteryMode.Hearts);
        var first = engine.SelectWinner();
        Assert.Equal("Sjokolade", first.Prize);
        Assert.Equal(first.Id, engine.SelectWinner().Id);
        Assert.Single(engine.State.ActiveRound!.Draws);
        engine.RevealWinner();
        Assert.Equal("Hovedgevinst", engine.SelectWinner().Prize);
        Assert.Equal(2, engine.State.ActiveRound!.Draws.Count);
    }

    [Fact]
    public void HeartNumbersResetOnlyForANewRound()
    {
        var engine = Create();
        engine.StartRound(LotteryMode.Hearts);
        Assert.Equal(1, engine.SelectWinner().Number);
        engine.RevealWinner();
        Assert.Equal(2, engine.SelectWinner().Number);
        engine.RevealWinner();
        Assert.Throws<InvalidOperationException>(() => engine.SelectWinner());
        engine.EndRound();
        engine.StartRound(LotteryMode.Hearts);
        Assert.Equal(1, engine.SelectWinner().Number);
    }

    [Fact]
    public void BookWinnersRemainExcludedAcrossRoundsAndRestart()
    {
        var store = new MemoryStore();
        var engine = Create(store);
        engine.StartRound(LotteryMode.Books);
        Assert.Equal(1, engine.SelectWinner().Number);
        engine.RevealWinner();
        engine.EndRound();
        engine = new LotteryEngine(store, _ => 0);
        engine.StartRound(LotteryMode.Books);
        Assert.Equal(2, engine.SelectWinner().Number);
    }

    [Fact]
    public void BookExclusionsAndPartialBooksUseGlobalNumbers()
    {
        var tickets = TicketRules.EligibleBooks(new BookSettings
        {
            FirstTicket = 1,
            LastTicket = 125,
            UnsoldBooks = "1",
            UnsoldTickets = "51-70, 72; 124-125"
        }, [71, 100]);
        Assert.Equal(73, tickets.First());
        Assert.Equal(123, tickets.Last());
        Assert.DoesNotContain(100, tickets);
        Assert.Equal(50, tickets.Length);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(50, 1)]
    [InlineData(51, 2)]
    [InlineData(73, 2)]
    [InlineData(1000, 20)]
    public void BookMappingUsesFiftyTickets(int number, int book) =>
        Assert.Equal(book, new DrawRecord { Number = number, Prize = "Gevinst" }.Book);

    [Theory]
    [InlineData("0")]
    [InlineData("9-3")]
    [InlineData("1-101")]
    [InlineData("abc")]
    [InlineData("999999999999")]
    [InlineData("1--2")]
    public void InvalidRangesAreRejected(string text) =>
        Assert.Throws<ArgumentException>(() => TicketRules.ParseRanges(text, 1, 100));

    [Fact]
    public void FailedSaveDoesNotConsumeNumberOrAdvancePrize()
    {
        var store = new MemoryStore();
        var engine = Create(store);
        engine.StartRound(LotteryMode.Hearts);
        store.Fail = true;
        Assert.Throws<IOException>(() => engine.SelectWinner());
        Assert.Empty(engine.State.ActiveRound!.Draws);
        store.Fail = false;
        Assert.Equal(1, engine.SelectWinner().Number);
    }

    [Fact]
    public void RecoveryReturnsTheSamePendingWinnerWithoutChoosingAgain()
    {
        var store = new MemoryStore();
        var engine = Create(store);
        engine.StartRound(LotteryMode.Books);
        var selected = engine.SelectWinner();
        var recovered = new LotteryEngine(store, _ => throw new Exception("Must not choose again"));
        Assert.Equal(selected.Id, recovered.SelectWinner().Id);
        Assert.Equal(selected.Number, recovered.SelectWinner().Number);
        Assert.Throws<InvalidOperationException>(() => recovered.EndRound());
    }

    [Fact]
    public void CannotStartOrResetWhileARoundIsActive()
    {
        var engine = Create();
        engine.StartRound(LotteryMode.Hearts);
        Assert.Throws<InvalidOperationException>(() => engine.StartRound(LotteryMode.Books));
        Assert.Throws<InvalidOperationException>(() => engine.NewEvent());
    }

    [Fact]
    public void TooManyPrizesAndEmptyEligiblePoolAreRejected()
    {
        var engine = Create();
        engine.SaveSettings(engine.State.Settings with { Books = new() { LastTicket = 1 } });
        Assert.Throws<InvalidOperationException>(() => engine.StartRound(LotteryMode.Books));
        engine.SaveSettings(engine.State.Settings with { Books = new() { LastTicket = 2, UnsoldBooks = "1" } });
        Assert.Throws<InvalidOperationException>(() => engine.StartRound(LotteryMode.Books));
    }

    [Fact]
    public void PartiallySoldBooksDoNotChangeIndividualTicketOdds()
    {
        var store = new MemoryStore();
        var settings = new AppSettings { BookPrizes = "Premie", Books = new() { LastTicket = 100, UnsoldTickets = "2-50" } };
        var bounds = 0;
        var engine = new LotteryEngine(store, count => { bounds = count; return count - 1; });
        engine.SaveSettings(settings);
        engine.StartRound(LotteryMode.Books);
        Assert.Equal(100, engine.SelectWinner().Number);
        Assert.Equal(51, bounds);
    }

    [Fact]
    public void NewEventResetsBookEligibilityButRetainsSettings()
    {
        var engine = Create();
        var previousId = engine.State.Id;
        engine.StartRound(LotteryMode.Books);
        engine.SelectWinner();
        engine.RevealWinner();
        engine.EndRound();
        engine.NewEvent();
        Assert.NotEqual(previousId, engine.State.Id);
        Assert.Empty(engine.State.Rounds);
        engine.StartRound(LotteryMode.Books);
        Assert.Equal(1, engine.SelectWinner().Number);
    }

    [Fact]
    public void ActiveRoundKeepsItsPrizeAndEligibilitySnapshot()
    {
        var engine = Create();
        engine.StartRound(LotteryMode.Books);
        engine.SaveSettings(engine.State.Settings with { BookPrizes = "Endret", Books = new() { FirstTicket = 99 } });
        Assert.Equal("Kake", engine.SelectWinner().Prize);
        Assert.Equal(1, engine.State.ActiveRound!.Draws[0].Number);
    }
}
