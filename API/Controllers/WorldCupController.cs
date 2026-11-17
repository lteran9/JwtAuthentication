using API.Data;
using API.WorldCup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("world-cup")]
public class WorldCupController(AppDbContext db) : ControllerBase
{
    private const int FirstWorldCupYear = 1930;

    [HttpGet("matches")]
    public async Task<ActionResult<IReadOnlyList<WorldCupMatchDto>>> GetMatches(
        [FromQuery] int? year,
        [FromQuery] string? team,
        CancellationToken cancellationToken)
    {
        var lastYear = DateTime.UtcNow.Year;
        if (year is { } requestedYear && (requestedYear < FirstWorldCupYear || requestedYear > lastYear))
        {
            ModelState.AddModelError(nameof(year), $"Year must be between {FirstWorldCupYear} and {lastYear}.");
            return ValidationProblem();
        }

        var matches = db.WorldCupMatches.AsNoTracking().AsQueryable();

        if (year is { } filterYear)
        {
            // A date range rather than DateOnly.Year keeps the query simple to translate to SQLite.
            var yearStart = new DateOnly(filterYear, 1, 1);
            var nextYearStart = new DateOnly(filterYear + 1, 1, 1);
            matches = matches.Where(match => match.MatchDate >= yearStart && match.MatchDate < nextYearStart);
        }

        if (!string.IsNullOrWhiteSpace(team))
        {
            var teamName = team.Trim().ToLowerInvariant();
            matches = matches.Where(match =>
                match.HomeTeam.ToLower() == teamName || match.AwayTeam.ToLower() == teamName);
        }

        return await matches
            .OrderByDescending(match => match.MatchDate)
            .ThenBy(match => match.Id)
            .Select(match => new WorldCupMatchDto(
                match.MatchDate,
                match.HomeTeam,
                match.HomeScore,
                match.AwayTeam,
                match.AwayScore,
                match.City,
                match.Country))
            .ToListAsync(cancellationToken);
    }
}
