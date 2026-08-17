using System.ComponentModel.DataAnnotations;
using AccountWeb.Services;
using Database;
using Database.Sqlite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AccountWeb.Pages;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly SqliteDbContext _db;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(SqliteDbContext db, ILogger<ResetPasswordModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Success { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Success)
        {
            return Page();
        }

        var resetToken = await LoadValidTokenAsync(cancellationToken);
        if (resetToken == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var resetToken = await LoadValidTokenAsync(cancellationToken);
        if (resetToken == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var pwnedPasswordResult = await PwnedPasswordValidator.CheckAsync(Input.Password, cancellationToken);
        if (pwnedPasswordResult.IsUnavailable)
        {
            ModelState.AddModelError(nameof(Input.Password), "Password safety validation is temporarily unavailable. Please try again.");
            return Page();
        }

        if (pwnedPasswordResult.IsPwned)
        {
            ModelState.AddModelError(
                nameof(Input.Password),
                $"This password has appeared in known breaches {pwnedPasswordResult.BreachCount:N0} times. Choose a different password.");
            return Page();
        }

        var hashedPassword = AccountPasswordHasher.HashPlainTextPasswordForStorage(Input.Password);
        if (hashedPassword == null)
        {
            ModelState.AddModelError(nameof(Input.Password), "Password is required.");
            return Page();
        }

        var accountName = resetToken.AccountNavigation.Name;
        resetToken.AccountNavigation.Password = hashedPassword;
        _db.ResetTokens.Remove(resetToken);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Password reset completed for account {AccountId} ({AccountName}); reset token {ResetTokenId} removed.",
            resetToken.AccountId,
            accountName,
            resetToken.ResetTokenId);

        return RedirectToPage("/ResetPassword", new { success = true });
    }

    private async Task<ResetToken?> LoadValidTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            return null;
        }

        var token = Token.Trim();
        return await _db.ResetTokens
            .Include(resetToken => resetToken.AccountNavigation)
            .FirstOrDefaultAsync(resetToken =>
                resetToken.Token == token &&
                !resetToken.TokenUsed &&
                resetToken.ExpireDate > DateTime.Now,
                cancellationToken);
    }

    public sealed class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
