using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApiOAuthTest.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class AccountController : ControllerBase
{
    [HttpGet]
    public IActionResult GoogleAuth()
    {
        var prop = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(this.GoogleAuthCallback))
        };

        return this.Challenge(prop, GoogleDefaults.AuthenticationScheme);
    }

    private GoogleAccount RegisterGoogleAccount(string identifier)
    {
        var email = this.User.FindFirst(ClaimTypes.Email)?.Value!;

        var userId = Guid.NewGuid();

        var googleAccount = new GoogleAccount()
        {
            Identifier = identifier,
            Email = email,
            UserId = userId
        };

        return googleAccount;
    }

    [HttpGet]
    [Authorize]
    public IActionResult GoogleAuthCallback()
    {
        var identifierClaim = this.User.FindAll(ClaimTypes.NameIdentifier)
            .FirstOrDefault(x => x.Issuer == "Google");

        if (identifierClaim is null)
        {
            return this.RedirectToAction("Error", "Home");
        }


        return this.Redirect("https://localhost:7081/");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await this.HttpContext.SignOutAsync();
        return this.RedirectToAction("Index", "Home");
    }
}

public class GoogleAccount
{
    public Guid UserId { get; init; }
    public string Identifier { get; init; } = default!;
    public string Email { get; init; } = default!;
}