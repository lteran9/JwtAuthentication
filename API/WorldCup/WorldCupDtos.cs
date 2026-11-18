namespace API.WorldCup;

public sealed record WorldCupMatchDto(
    DateOnly Date,
    string HomeTeam,
    int HomeScore,
    string AwayTeam,
    int AwayScore,
    string? PenaltyWinner,
    string City,
    string Country);

/// <summary>A page of World Cup match results and its pagination metadata.</summary>
public sealed record WorldCupMatchesPageDto(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<WorldCupMatchDto> Matches);
