namespace API.WorldCup;

public sealed record WorldCupMatchDto(
    DateOnly Date,
    string HomeTeam,
    int HomeScore,
    string AwayTeam,
    int AwayScore,
    string City,
    string Country);
