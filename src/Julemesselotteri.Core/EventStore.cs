using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Julemesselotteri.Core;

public interface IEventStore
{
    EventState? Load();
    void Save(EventState state);
}

public sealed class EventFileStore(string dataDirectory, string logDirectory) : IEventStore
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        IgnoreReadOnlyProperties = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string StatePath => Path.Combine(dataDirectory, "state.json");
    public string LogDirectory => logDirectory;

    public EventState? Load()
    {
        if (!File.Exists(StatePath))
        {
            if (File.Exists(StatePath + ".bak"))
                throw new InvalidDataException("Tilstandsfilen mangler, men en sikkerhetskopi finnes. Gjenopprett før nye trekninger.");
            return null;
        }
        try
        {
            var state = JsonSerializer.Deserialize<EventState>(File.ReadAllText(StatePath), JsonOptions)
                ?? throw new InvalidDataException("Tom tilstandsfil.");
            ValidateState(state);
            return state;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or NullReferenceException)
        {
            throw new InvalidDataException("Tilstandsfilen kan ikke leses. Ikke start på nytt før dataene er kontrollert.", ex);
        }
    }

    private static void ValidateState(EventState state)
    {
        if (state.SchemaVersion != 1 || state.Id == Guid.Empty || state.CreatedAt == default ||
            state.Rounds is null || state.Rounds.Count(r => r.EndedAt is null) > 1)
            throw new InvalidDataException("Ukjent eller ugyldig tilstandsformat.");
        TicketRules.ValidateSettings(state.Settings);
        var bookWinners = new HashSet<int>();
        foreach (var round in state.Rounds)
        {
            if (!Enum.IsDefined(round.Mode) || round.Id == Guid.Empty ||
                round.Prizes is null || round.Prizes.Count == 0 || round.Prizes.Any(string.IsNullOrWhiteSpace) ||
                round.Draws is null || round.Draws.Count > round.Prizes.Count)
                throw new InvalidDataException("Ugyldige rundedata.");
            var eligible = round.Mode == LotteryMode.Books
                ? TicketRules.EligibleBooks(round.Books, []).ToHashSet()
                : Enumerable.Range(1, 1000).ToHashSet();
            var seen = new HashSet<int>();
            for (var i = 0; i < round.Draws.Count; i++)
            {
                var draw = round.Draws[i];
                if (!eligible.Contains(draw.Number) || !seen.Add(draw.Number) ||
                    (round.Mode == LotteryMode.Books && !bookWinners.Add(draw.Number)) ||
                    draw.Prize != round.Prizes[i] || draw.Id == Guid.Empty ||
                    (draw.RevealedAt is null && (i != round.Draws.Count - 1 || round.EndedAt is not null)))
                    throw new InvalidDataException("Ugyldige eller gjentatte vinnernummer.");
            }
        }
    }

    public void Save(EventState state)
    {
        Directory.CreateDirectory(dataDirectory);
        var temporary = StatePath + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                   4096, FileOptions.WriteThrough))
        {
            JsonSerializer.Serialize(stream, state, JsonOptions);
            stream.Flush(true);
        }
        if (File.Exists(StatePath))
            File.Replace(temporary, StatePath, StatePath + ".bak");
        else
            File.Move(temporary, StatePath);
    }

    // Logs are a recoverable projection of the durable state, never the source of draw eligibility.
    public void WriteLogs(EventState state)
    {
        Directory.CreateDirectory(logDirectory);
        foreach (var round in state.Rounds)
        {
            var kind = round.Mode == LotteryMode.Hearts ? "Hjertelodd" : "Boklodd";
            var filename = $"{round.StartedAt:yyyyMMdd-HHmmss}_{kind}_{round.Id:N}.txt";
            var text = new StringBuilder()
                .AppendLine("JULEMESSELOTTERI")
                .AppendLine($"Arrangement: {state.Id}")
                .AppendLine($"Runde: {round.Id}")
                .AppendLine($"Lotteri: {kind}")
                .AppendLine($"Startet: {round.StartedAt:yyyy-MM-dd HH:mm:ss zzz}")
                .AppendLine($"Avsluttet: {round.EndedAt?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "Pågår"}")
                .AppendLine();
            foreach (var draw in round.Draws)
            {
                text.AppendLine($"Tid: {draw.SelectedAt:yyyy-MM-dd HH:mm:ss zzz}")
                    .AppendLine($"Gevinst: {draw.Prize}")
                    .AppendLine($"Loddnummer: {draw.Number}");
                if (round.Mode == LotteryMode.Books)
                    text.AppendLine($"Bok: {draw.Book}");
                text.AppendLine(draw.RevealedAt is null ? "Status: Valgt, venter på visning" : "Status: Vist")
                    .AppendLine();
            }
            var path = Path.Combine(logDirectory, filename);
            File.WriteAllText(path + ".tmp", text.ToString(), Encoding.UTF8);
            File.Move(path + ".tmp", path, true);
        }
    }

    internal static EventState Copy(EventState state) =>
        JsonSerializer.Deserialize<EventState>(JsonSerializer.Serialize(state, JsonOptions), JsonOptions)!;
}
