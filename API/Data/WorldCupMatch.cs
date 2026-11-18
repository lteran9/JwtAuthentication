namespace API.Data;

public class WorldCupMatch
{
    public int Id { get; set; }
    public DateOnly MatchDate { get; set; }
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string? PenaltyWinner { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
}
