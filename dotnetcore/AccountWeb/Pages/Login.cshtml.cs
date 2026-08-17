using System.ComponentModel.DataAnnotations;
using AccountWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AccountWeb.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AccountSignInService _signInService;

    public LoginModel(AccountSignInService signInService)
    {
        _signInService = signInService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(GetSafeReturnUrl());
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var account = await _signInService.ValidateAndUpdateLoginAsync(
            Input.Username,
            Input.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (account == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username, password, or account level.");
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AccountSignInService.CreatePrincipal(account),
            AccountSignInService.CreateAuthenticationProperties(Input.RememberMe));

        return LocalRedirect(GetSafeReturnUrl());
    }

    private string GetSafeReturnUrl()
    {
        return Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Page("/Index")!;
    }

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
