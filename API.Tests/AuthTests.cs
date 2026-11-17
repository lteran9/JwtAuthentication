using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace API.Tests;

public sealed class AuthTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AuthTests(ApiFactory factory)
    {
        // HTTPS base address avoids the HTTP-to-HTTPS redirect from UseHttpsRedirection.
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsAccessToken()
    {
        var accessToken = await TestAuth.RegisterAndLoginAsync(_client);

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var userName = $"user-{Guid.NewGuid():N}";
        var registerResponse = await _client.PostAsJsonAsync("auth/register", new { userName, password = TestAuth.Password });
        registerResponse.EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("auth/login", new { userName, password = "Wrong-Password-1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
