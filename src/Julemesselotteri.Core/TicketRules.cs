using System.Globalization;
using System.Text.RegularExpressions;

namespace Julemesselotteri.Core;

public static partial class TicketRules
{
    public const int MaximumTicket = 1_000_000;

    [GeneratedRegex(@"^(\d+)(?:-(\d+))?$")]
    private static partial Regex RangePattern();

    public static HashSet<int> ParseRanges(string text, int minimum, int maximum)
    {
        var result = new HashSet<int>();
        foreach (var token in Regex.Split(text.Trim(), @"[,;\s]+").Where(t => t.Length > 0))
        {
            var match = RangePattern().Match(token);
            if (!match.Success ||
                !int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var first) ||
                !int.TryParse(match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value,
                    CultureInfo.InvariantCulture, out var last) ||
                first < minimum || last > maximum || first > last)
            {
                throw new ArgumentException($"Ugyldig intervall: «{token}». Bruk tall eller fra-til mellom {minimum} og {maximum}.");
            }

            for (var number = first; number <= last; number++)
                result.Add(number);
        }

        return result;
    }

    public static int[] EligibleBooks(BookSettings settings, IEnumerable<int> winners)
    {
        if (settings.FirstTicket < 1 || settings.LastTicket < settings.FirstTicket ||
            settings.LastTicket > MaximumTicket)
            throw new ArgumentException($"Loddintervallet må være mellom 1 og {MaximumTicket}, med fra ≤ til.");

        var books = ParseRanges(settings.UnsoldBooks, 1, (settings.LastTicket - 1) / 50 + 1);
        var excluded = ParseRanges(settings.UnsoldTickets, settings.FirstTicket, settings.LastTicket);
        excluded.UnionWith(winners);
        return Enumerable.Range(settings.FirstTicket, settings.LastTicket - settings.FirstTicket + 1)
            .Where(n => !books.Contains((n - 1) / 50 + 1) && !excluded.Contains(n)).ToArray();
    }

    public static List<string> PrizeOrder(string text) => text
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(p => p.Length > 0).Reverse().ToList();

    public static void ValidateSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settings.Books);
        ArgumentNullException.ThrowIfNull(settings.HeartPrizes);
        ArgumentNullException.ThrowIfNull(settings.BookPrizes);
        ArgumentNullException.ThrowIfNull(settings.DisplayDevice);
        _ = EligibleBooks(settings.Books, []);
        foreach (var animation in new[] { settings.HeartsAnimation, settings.BooksAnimation })
        {
            if (animation is null || !double.IsFinite(animation.DurationSeconds) ||
                !double.IsFinite(animation.BookPauseSeconds) ||
                animation.DurationSeconds is < 0.5 or > 60 || animation.TickMilliseconds is < 30 or > 500 ||
                animation.BookPauseSeconds is < 0 or > 30)
                throw new ArgumentException("Varighet må være 0,5–60 s, tallbytte 30–500 ms og bokpause 0–30 s.");
        }
        if (settings.Volume is < 0 or > 100)
            throw new ArgumentException("Volum må være mellom 0 og 100.");
    }
}
