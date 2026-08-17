using System.Security.Claims;
using AccountWeb.Services;
using Database.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AccountWeb.Pages;

public class IndexModel : PageModel
{
    private readonly SqliteDbContext _db;

    public IndexModel(SqliteDbContext db)
    {
        _db = db;
    }

    public string AccountName { get; private set; } = "";

    public string Email { get; private set; } = "";

    public int Permission { get; private set; }

    public DateTime LastAccess { get; private set; }

    public int AccountsCount { get; private set; }

    public int ZonesCount { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    public IReadOnlyList<SearchResult> SearchResults { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var accountIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(accountIdValue, out var accountId))
        {
            var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
            if (account != null)
            {
                AccountName = account.Name;
                Email = account.Email;
                Permission = account.Permission;
                LastAccess = account.LastAccess;
            }
        }

        AccountsCount = await _db.Accounts.CountAsync(cancellationToken);
        ZonesCount = await _db.Zones.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(Q))
        {
            SearchResults = await SearchAsync(Q.Trim(), cancellationToken);
        }
    }

    private async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var pattern = $"%{EscapeLikePattern(query)}%";

        var accountResults = await _db.Accounts
            .AsNoTracking()
            .Where(account =>
                EF.Functions.Like(account.Name, pattern, @"\") ||
                EF.Functions.Like(account.Email, pattern, @"\"))
            .OrderBy(account => account.Name)
            .Take(50)
            .Select(account => new SearchResult(
                "Account",
                account.AccountId,
                account.Name,
                account.Email,
                account.Permission,
                "",
                null,
                account.LastAccess,
                "/accounts/" + account.AccountId))
            .ToListAsync(cancellationToken);

        var aliasResults = await _db.Aliases
            .AsNoTracking()
            .Include(alias => alias.AccountNavigation)
            .Where(alias =>
                EF.Functions.Like(alias.Name, pattern, @"\") ||
                EF.Functions.Like(alias.AccountNavigation.Name, pattern, @"\") ||
                EF.Functions.Like(alias.AccountNavigation.Email, pattern, @"\"))
            .OrderBy(alias => alias.Name)
            .Take(50)
            .Select(alias => new SearchResult(
                "Alias",
                alias.AliasId,
                alias.Name,
                alias.AccountNavigation.Email,
                alias.AccountNavigation.Permission,
                alias.AccountNavigation.Name,
                alias.AccountId,
                alias.LastAccess,
                "/aliases/" + alias.AliasId))
            .ToListAsync(cancellationToken);

        return accountResults
            .Concat(aliasResults)
            .OrderBy(result => result.Name)
            .ThenBy(result => result.Type)
            .ToList();
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }

    public sealed record SearchResult(
        string Type,
        long Id,
        string Name,
        string Email,
        int Level,
        string Owner,
        long? OwnerAccountId,
        DateTime LastAccess,
        string DetailsPath);
}
