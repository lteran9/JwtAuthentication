using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace API.Data;

// Loads the embedded World Cup CSV once, when the table is empty. The CSV is a FIFA World Cup subset of martj42/international_results (CC0 1.0).
public static class WorldCupSeeder
{
    private const string ResourceName = "worldcup_matches.csv";

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var matches = ReadMatches();

        if (!await db.WorldCupMatches.AnyAsync(cancellationToken))
        {
            db.WorldCupMatches.AddRange(matches);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var existingMatches = await db.WorldCupMatches.ToListAsync(cancellationToken);
        foreach (var existingMatch in existingMatches)
        {
            var seededMatch = matches.FirstOrDefault(match =>
                match.MatchDate == existingMatch.MatchDate &&
                match.HomeTeam == existingMatch.HomeTeam &&
                match.AwayTeam == existingMatch.AwayTeam);

            if (seededMatch is not null)
            {
                existingMatch.PenaltyWinner = seededMatch.PenaltyWinner;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<WorldCupMatch> ReadMatches()
    {
        using var stream = typeof(WorldCupSeeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var parser = new TextFieldParser(stream);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        // Look up columns by header name so the parser doesn't depend on column order.
        var header = parser.ReadFields() ?? throw new InvalidDataException($"'{ResourceName}' is empty.");
        var columns = header.Select((name, index) => (name, index)).ToDictionary(column => column.name, column => column.index);

        var matches = new List<WorldCupMatch>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields()!;
            matches.Add(new WorldCupMatch
            {
                MatchDate = DateOnly.ParseExact(fields[columns["date"]], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                HomeTeam = fields[columns["home_team"]],
                AwayTeam = fields[columns["away_team"]],
                HomeScore = int.Parse(fields[columns["home_score"]], CultureInfo.InvariantCulture),
                AwayScore = int.Parse(fields[columns["away_score"]], CultureInfo.InvariantCulture),
                PenaltyWinner = string.IsNullOrWhiteSpace(fields[columns["penalty_winner"]])
                    ? null
                    : fields[columns["penalty_winner"]],
                City = fields[columns["city"]],
                Country = fields[columns["country"]]
            });
        }

        return matches;
    }
}
