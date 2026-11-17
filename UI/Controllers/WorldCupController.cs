using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Api;
using UI.Models;

namespace UI.Controllers;

[Authorize]
public class WorldCupController(ApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? year, string? team, CancellationToken cancellationToken)
    {
        var model = new WorldCupViewModel { Year = year, Team = team };

        try
        {
            // The API treats a blank team as "no team filter".
            model.Matches = await apiClient.MatchesAsync(year, team ?? string.Empty, cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status401Unauthorized)
        {
            // The API rejected the stored token, so end the local session and ask the user to sign in again.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
        {
            ModelState.AddModelError(string.Empty, "Enter a year from 1930 up to the current year.");
        }

        return View(model);
    }
}
