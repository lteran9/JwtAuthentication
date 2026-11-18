using UI.Api;

namespace UI.Models;

public class WorldCupViewModel
{
    public int? Year { get; init; }

    public string? Team { get; init; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public ICollection<WorldCupMatchDto> Matches { get; set; } = [];
}
