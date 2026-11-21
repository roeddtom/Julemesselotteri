using Julemesselotteri.Core;

namespace Julemesselotteri.Core.Tests;

public sealed class FileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Julemesselotteri-tests-" + Guid.NewGuid());
    private EventFileStore Store => new(_directory, Path.Combine(_directory, "logs"));

    [Fact]
    public void FileRestartPreservesPendingWinnerAndRegeneratesReadableLog()
    {
        var store = Store;
        var engine = new LotteryEngine(store, _ => 72);
        engine.SaveSettings(new AppSettings { BookPrizes = "Sykkel" });
        engine.StartRound(LotteryMode.Books);
        var draw = engine.SelectWinner();
        var restored = new LotteryEngine(store);
        Assert.Equal(draw.Id, restored.State.ActiveRound!.PendingDraw!.Id);
        Assert.Equal(73, restored.State.ActiveRound.PendingDraw.Number);
        store.WriteLogs(restored.State);
        var log = File.ReadAllText(Assert.Single(Directory.GetFiles(store.LogDirectory, "*.txt")));
        Assert.Contains("Loddnummer: 73", log);
        Assert.Contains("Bok: 2", log);
        Assert.Contains("venter på visning", log);
        restored.RevealWinner();
        store.WriteLogs(restored.State);
        Assert.Contains("Status: Vist", File.ReadAllText(Directory.GetFiles(store.LogDirectory, "*.txt")[0]));
        Assert.True(File.Exists(store.StatePath + ".bak"));
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"SchemaVersion\":999}")]
    public void CorruptStateNeverSilentlyCreatesANewEvent(string json)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Store.StatePath, json);
        Assert.Throws<InvalidDataException>(() => new LotteryEngine(Store));
        Assert.Equal(json, File.ReadAllText(Store.StatePath));
    }

    [Fact]
    public void MissingPrimaryWithBackupRequiresManualRecovery()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Store.StatePath + ".bak", "{}");
        Assert.Throws<InvalidDataException>(() => Store.Load());
    }

    [Fact]
    public void LogFailureDoesNotLoseDurableSelection()
    {
        var engine = new LotteryEngine(Store, _ => 0);
        engine.SaveSettings(new AppSettings { HeartPrizes = "Gevinst" });
        engine.StartRound(LotteryMode.Hearts);
        var draw = engine.SelectWinner();
        var blockedDirectory = Path.Combine(_directory, "blocked");
        File.WriteAllText(blockedDirectory, "Not a directory");
        Assert.Throws<IOException>(() => new EventFileStore(_directory, blockedDirectory).WriteLogs(engine.State));
        Assert.Equal(draw.Id, new LotteryEngine(Store).State.ActiveRound!.PendingDraw!.Id);
    }

    [Fact]
    public void ExtraPrizeSurvivesFileRestartWithoutRerollAndContinuesNumbering()
    {
        var engine = new LotteryEngine(Store, _ => 0);
        engine.SaveSettings(new AppSettings { HeartPrizes = "Premie" });
        engine.StartRound(LotteryMode.Hearts);
        engine.SelectWinner();
        engine.RevealWinner();
        Assert.DoesNotContain("ExtraPrizeCount", File.ReadAllText(Store.StatePath));
        engine = new LotteryEngine(Store, _ => 0);
        var extra = engine.SelectWinner();

        var restored = new LotteryEngine(Store, _ => throw new Exception("Must not choose again"));
        Assert.Equal(extra.Id, restored.SelectWinner().Id);
        Assert.Equal("EKSTRAPREMIE 1", restored.State.ActiveRound!.PendingDraw!.Prize);
        restored.RevealWinner();
        restored = new LotteryEngine(Store, _ => 0);
        var second = restored.SelectWinner();
        Assert.Equal(("EKSTRAPREMIE 2", 3), (second.Prize, second.Number));
        Store.WriteLogs(restored.State);
        var log = File.ReadAllText(Assert.Single(Directory.GetFiles(Store.LogDirectory, "*.txt")));
        Assert.Contains("Gevinst: EKSTRAPREMIE 2", log);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }
}
