using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using UI.Api;
using UI.Models;

namespace UI.Controllers;

public class AccountController(ApiClient apiClient) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        LoginResponse login;
        try
        {
            login = await apiClient.LoginAsync(
                new LoginRequest { UserName = model.UserName, Password = model.Password },
                cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status401Unauthorized)
        {
            ModelState.AddModelError(string.Empty, "Invalid user name or password.");
            return View(model);
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, model.UserName)],
            CookieAuthenticationDefaults.AuthenticationScheme);

        // Tie the cookie lifetime to the token lifetime so the UI never holds a cookie with an expired token.
        var properties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = login.ExpiresAt
        };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = login.AccessToken }]);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        // Only redirect to local URLs to prevent open-redirect attacks.
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await apiClient.RegisterAsync(
                new RegisterRequest { UserName = model.UserName, Password = model.Password },
                cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
        {
            AddApiValidationErrors(exception.Response);
            return View(model);
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // The API returns Identity's messages (such as password rules) as a ValidationProblem with an "errors" object.
    private void AddApiValidationErrors(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        if (document.RootElement.TryGetProperty("errors", out var errors))
        {
            foreach (var field in errors.EnumerateObject())
            {
                foreach (var message in field.Value.EnumerateArray())
                {
                    if (message.GetString() is { } text)
                    {
                        ModelState.AddModelError(string.Empty, text);
                    }
                }
            }
        }

        if (ModelState.ErrorCount == 0)
        {
            ModelState.AddModelError(string.Empty, "Could not create the account. Try again.");
        }
    }
}
