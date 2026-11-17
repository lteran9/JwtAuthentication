using UI.Api;

namespace UI.Models;

public class WorldCupViewModel
{
    public int? Year { get; init; }

    public string? Team { get; init; }

    public ICollection<WorldCupMatchDto> Matches { get; set; } = [];
}
