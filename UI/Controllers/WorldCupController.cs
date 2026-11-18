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
    private const int PageSize = 25;

    [HttpGet]
    public async Task<IActionResult> Index(
        int? year,
        string? team,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new WorldCupViewModel
        {
            Year = year,
            Team = team,
            Page = page,
            PageSize = PageSize
        };

        if (page < 1)
        {
            ModelState.AddModelError(nameof(page), "Page number must be 1 or greater.");
            return View(model);
        }

        try
        {
            // The API treats a blank team as "no team filter".
            var result = await apiClient.MatchesAsync(
                year,
                team ?? string.Empty,
                page,
                PageSize,
                cancellationToken);
            model.Matches = result.Matches;
            model.Page = result.Page;
            model.PageSize = result.PageSize;
            model.TotalCount = result.TotalCount;
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
        {
            ModelState.AddModelError(string.Empty, "Enter a year from 1930 up to the current year.");
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status401Unauthorized)
        {
            // The API rejected the stored token, so end the local session and ask the user to sign in again.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        return View(model);
    }
}
