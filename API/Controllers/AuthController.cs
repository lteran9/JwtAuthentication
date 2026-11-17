using API.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(UserManager<IdentityUser> userManager, JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await userManager.CreateAsync(new IdentityUser { UserName = request.UserName }, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return ValidationProblem();
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.UserName);

        // Unknown user, locked-out user, and wrong password all return the same 401 so callers can't tell which one failed.
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return Unauthorized();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Unauthorized();
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var token = tokenService.CreateAccessToken(user);
        return new LoginResponse(token.Value, token.ExpiresAt);
    }
}
