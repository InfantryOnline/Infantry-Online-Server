using System.ComponentModel.DataAnnotations;
using AccountWeb.Services;
using Database;
using Database.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AccountWeb.Pages.Aliases;

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

    public Alias? Alias { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Alias = await LoadAliasAsync(cancellationToken);
        if (Alias == null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Name = Alias.Name
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Alias = await _db.Aliases
            .Include(alias => alias.AccountNavigation)
            .FirstOrDefaultAsync(alias => alias.AliasId == Id, cancellationToken);

        if (Alias == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var name = Input.Name.Trim();

        if (name.Length == 0)
        {
            ModelState.AddModelError("Input.Name", "Name is required.");
            return Page();
        }

        var nameExists = await _db.Aliases.AnyAsync(
            alias => alias.AliasId != Id && alias.Name == name,
            cancellationToken);

        if (nameExists)
        {
            ModelState.AddModelError("Input.Name", "Another alias already uses this name.");
            return Page();
        }

        var oldName = Alias.Name;
        Alias.Name = name;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The alias could not be saved. Check for duplicate or invalid values.");
            return Page();
        }

        _logger.LogInformation(
            "Alias {AliasId} edited for account {AccountId}: Name {OldName} -> {NewName}",
            Alias.AliasId,
            Alias.AccountId,
            oldName,
            Alias.Name);

        StatusMessage = "Alias updated.";
        return RedirectToPage(new { id = Id });
    }

    private Task<Alias?> LoadAliasAsync(CancellationToken cancellationToken)
    {
        return _db.Aliases
            .AsNoTracking()
            .Include(alias => alias.AccountNavigation)
            .FirstOrDefaultAsync(alias => alias.AliasId == Id, cancellationToken);
    }

    public sealed class InputModel
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";
    }
}
