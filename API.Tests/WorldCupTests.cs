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

        var matches = await client.GetFromJsonAsync<List<WorldCupMatchDto>>("world-cup/matches?year=2022");

        Assert.NotEmpty(matches!);
        Assert.All(matches!, match => Assert.Equal(2022, match.Date.Year));
    }

    [Fact]
    public async Task GetMatches_WithTeam_IsCaseInsensitiveAndMatchesHomeOrAway()
    {
        var client = await CreateAuthenticatedClientAsync();

        var matches = await client.GetFromJsonAsync<List<WorldCupMatchDto>>("world-cup/matches?team=ARGENTINA");

        Assert.NotEmpty(matches!);
        Assert.All(matches!, match => Assert.True(match.HomeTeam == "Argentina" || match.AwayTeam == "Argentina"));
        Assert.Contains(matches!, match => match.AwayTeam == "Argentina");
    }

    [Fact]
    public async Task GetMatches_ReturnsNewestFirst()
    {
        var client = await CreateAuthenticatedClientAsync();

        var matches = await client.GetFromJsonAsync<List<WorldCupMatchDto>>("world-cup/matches");

        var dates = matches!.Select(match => match.Date).ToList();
        Assert.Equal(dates.OrderByDescending(date => date).ToList(), dates);
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
