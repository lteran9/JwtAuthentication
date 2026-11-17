using System.Net.Http.Json;
using API.Auth;

namespace API.Tests;

public static class TestAuth
{
    public const string Password = "Test-Password-1!";

    public static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        var userName = $"user-{Guid.NewGuid():N}";

        var registerResponse = await client.PostAsJsonAsync("auth/register", new { userName, password = Password });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("auth/login", new { userName, password = Password });
        loginResponse.EnsureSuccessStatusCode();

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.AccessToken;
    }
}
