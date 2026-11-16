namespace Julemesselotteri.Core;

public enum LotteryMode { Hearts, Books }

public sealed record AnimationSettings
{
    public double DurationSeconds { get; init; } = 5;
    public int TickMilliseconds { get; init; } = 70;
    public double BookPauseSeconds { get; init; } = 2;
}

public sealed record BookSettings
{
    public int FirstTicket { get; init; } = 1;
    public int LastTicket { get; init; } = 1000;
    public string UnsoldBooks { get; init; } = "";
    public string UnsoldTickets { get; init; } = "";
}

public sealed record AppSettings
{
    public string HeartPrizes { get; init; } = "";
    public string BookPrizes { get; init; } = "";
    public BookSettings Books { get; init; } = new();
    public AnimationSettings HeartsAnimation { get; init; } = new();
    public AnimationSettings BooksAnimation { get; init; } = new() { DurationSeconds = 8 };
    public int Volume { get; init; } = 50;
    public bool Muted { get; init; }
    public string DisplayDevice { get; init; } = "";
}

public sealed class DrawRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Prize { get; init; }
    public required int Number { get; init; }
    public DateTimeOffset SelectedAt { get; init; } = DateTimeOffset.Now;
    public DateTimeOffset? RevealedAt { get; set; }
    public int Book => (Number - 1) / 50 + 1;
}

public sealed class LotteryRound
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required LotteryMode Mode { get; init; }
    public required List<string> Prizes { get; init; }
    public required BookSettings Books { get; init; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;
    public DateTimeOffset? EndedAt { get; set; }
    public List<DrawRecord> Draws { get; init; } = [];
    public DrawRecord? PendingDraw => Draws.LastOrDefault(d => d.RevealedAt is null);
}

public sealed class EventState
{
    public required int SchemaVersion { get; init; }
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required AppSettings Settings { get; set; }
    public required List<LotteryRound> Rounds { get; init; }
    public LotteryRound? ActiveRound => Rounds.SingleOrDefault(r => r.EndedAt is null);

    public static EventState Create(AppSettings? settings = null) => new()
    {
        SchemaVersion = 1,
        Id = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.Now,
        Settings = settings ?? new(),
        Rounds = []
    };
}
