using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace UI.Api;

// Adds the access token stored in the auth cookie to each API request. Anonymous requests (such as login) go out without the header.
public sealed class BearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = httpContextAccessor.HttpContext is { } httpContext
            ? await httpContext.GetTokenAsync("access_token")
            : null;

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
