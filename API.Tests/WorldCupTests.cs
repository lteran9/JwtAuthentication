using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.WorldCup;
using Microsoft.AspNetCore.Mvc.Testing;

namespace API.Tests;

public sealed class WorldCupTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public WorldCupTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMatches_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("world-cup/matches");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMatches_WithYear_ReturnsOnlyMatchesFromThatYear()
    {
        var client = await CreateAuthenticatedClientAsync();

        var result = await client.GetFromJsonAsync<WorldCupMatchesPageDto>("world-cup/matches?year=2022");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Matches);
        Assert.All(result.Matches, match => Assert.Equal(2022, match.Date.Year));
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetMatches_WithTeam_IsCaseInsensitiveAndMatchesHomeOrAway()
    {
        var client = await CreateAuthenticatedClientAsync();

        var result = await client.GetFromJsonAsync<WorldCupMatchesPageDto>("world-cup/matches?team=ARGENTINA");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Matches);
        Assert.All(result.Matches, match => Assert.True(match.HomeTeam == "Argentina" || match.AwayTeam == "Argentina"));
        Assert.Contains(result.Matches, match => match.AwayTeam == "Argentina");
    }

    [Fact]
    public async Task GetMatches_ReturnsPenaltyWinnerForShootoutMatch()
    {
        var client = await CreateAuthenticatedClientAsync();

        var result = await client.GetFromJsonAsync<WorldCupMatchesPageDto>(
            "world-cup/matches?year=2022&pageSize=100");

        Assert.NotNull(result);
        var final = Assert.Single(result.Matches, match =>
            match.HomeTeam == "Argentina" &&
            match.AwayTeam == "France");
        Assert.Equal("Argentina", final.PenaltyWinner);
    }

    [Fact]
    public async Task GetMatches_ReturnsNewestFirst()
    {
        var client = await CreateAuthenticatedClientAsync();

        var result = await client.GetFromJsonAsync<WorldCupMatchesPageDto>("world-cup/matches");

        Assert.NotNull(result);
        var dates = result.Matches.Select(match => match.Date).ToList();
        Assert.Equal(dates.OrderByDescending(date => date).ToList(), dates);
    }

    [Fact]
    public async Task GetMatches_ReturnsRequestedPageAndTotalCount()
    {
        var client = await CreateAuthenticatedClientAsync();

        var firstPage = await client.GetFromJsonAsync<WorldCupMatchesPageDto>(
            "world-cup/matches?page=1&pageSize=5");
        var secondPage = await client.GetFromJsonAsync<WorldCupMatchesPageDto>(
            "world-cup/matches?page=2&pageSize=5");

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(5, firstPage.Matches.Count);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(5, secondPage.PageSize);
        Assert.Equal(firstPage.TotalCount, secondPage.TotalCount);

        var firstPageMatches = firstPage.Matches.Select(match => (match.Date, match.HomeTeam, match.AwayTeam));
        var secondPageMatches = secondPage.Matches.Select(match => (match.Date, match.HomeTeam, match.AwayTeam));
        Assert.Empty(firstPageMatches.Intersect(secondPageMatches));
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task GetMatches_WithInvalidPagination_Returns400(string query)
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"world-cup/matches?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMatches_WithPageBeyondResults_ReturnsEmptyPageWithoutOverflow()
    {
        var client = await CreateAuthenticatedClientAsync();

        var result = await client.GetFromJsonAsync<WorldCupMatchesPageDto>(
            $"world-cup/matches?page={int.MaxValue}&pageSize=100");

        Assert.NotNull(result);
        Assert.Equal(int.MaxValue, result.Page);
        Assert.True(result.TotalCount > 0);
        Assert.Empty(result.Matches);
    }

    [Theory]
    [InlineData(1929)]
    [InlineData(2100)]
    public async Task GetMatches_WithYearOutsideTournamentRange_Returns400(int year)
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"world-cup/matches?year={year}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var accessToken = await TestAuth.RegisterAndLoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
