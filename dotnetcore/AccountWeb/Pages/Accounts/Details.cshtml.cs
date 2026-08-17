using System.ComponentModel.DataAnnotations;
using AccountWeb.Services;
using Database;
using Database.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AccountWeb.Pages.Accounts;

public class DetailsModel : PageModel
{
    private readonly SqliteDbContext _db;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(SqliteDbContext db, ILogger<DetailsModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public long Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Account? Account { get; private set; }

    public IReadOnlyList<AliasSummary> Aliases { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.AccountId == Id, cancellationToken);

        if (Account == null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Name = Account.Name,
            Permission = Account.Permission,
            Email = Account.Email
        };

        await LoadAliasesAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Account = await _db.Accounts.FirstOrDefaultAsync(account => account.AccountId == Id, cancellationToken);
        if (Account == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadAliasesAsync(cancellationToken);
            return Page();
        }

        var name = Input.Name.Trim();
        var email = Input.Email.Trim();

        if (name.Length == 0)
        {
            ModelState.AddModelError("Input.Name", "Name is required.");
            await LoadAliasesAsync(cancellationToken);
            return Page();
        }

        if (email.Length == 0)
        {
            ModelState.AddModelError("Input.Email", "Email is required.");
            await LoadAliasesAsync(cancellationToken);
            return Page();
        }

        var nameExists = await _db.Accounts.AnyAsync(
            account => account.AccountId != Id && account.Name == name,
            cancellationToken);

        if (nameExists)
        {
            ModelState.AddModelError("Input.Name", "Another account already uses this name.");
            await LoadAliasesAsync(cancellationToken);
            return Page();
        }

        var emailExists = await _db.Accounts.AnyAsync(
            account => account.AccountId != Id && account.Email == email,
            cancellationToken);

        if (emailExists)
        {
            ModelState.AddModelError("Input.Email", "Another account already uses this email.");
            await LoadAliasesAsync(cancellationToken);
            return Page();
        }

        var oldName = Account.Name;
        var oldPermission = Account.Permission;
        var oldEmail = Account.Email;

        Account.Name = name;
        Account.Permission = Input.Permission;
        Account.Email = email;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The account could not be saved. Check for duplicate or invalid values.");
            await LoadAliasesAsync(cancellationToken);
            return Page();
        }

        _logger.LogInformation(
            "Account {AccountId} edited: Name {OldName} -> {NewName}; Level {OldLevel} -> {NewLevel}; Email {OldEmail} -> {NewEmail}",
            Account.AccountId,
            oldName,
            Account.Name,
            oldPermission,
            Account.Permission,
            oldEmail,
            Account.Email);

        StatusMessage = "Account updated.";
        return RedirectToPage(new { id = Id });
    }

    private async Task LoadAliasesAsync(CancellationToken cancellationToken)
    {
        Aliases = await _db.Aliases
            .AsNoTracking()
            .Where(alias => alias.AccountId == Id)
            .OrderBy(alias => alias.Name)
            .Select(alias => new AliasSummary(alias.AliasId, alias.Name, alias.LastAccess, alias.TimePlayed))
            .ToListAsync(cancellationToken);
    }

    public sealed class InputModel
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";

        [Range(0, 5)]
        [Display(Name = "Level")]
        public int Permission { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = "";
    }

    public sealed record AliasSummary(long AliasId, string Name, DateTime LastAccess, long TimePlayed);
}
