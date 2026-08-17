using System.Security.Claims;
using Database;
using Database.Sqlite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace AccountWeb.Services;

public sealed class AccountSignInService
{
    private const int RequiredAccountLevel = 4;

    private readonly SqliteDbContext _db;

    public AccountSignInService(SqliteDbContext db)
    {
        _db = db;
    }

    public async Task<Account?> ValidateAndUpdateLoginAsync(
        string username,
        string password,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == username, cancellationToken);
        if (account == null)
        {
            return null;
        }

        if (account.Permission < RequiredAccountLevel)
        {
            return null;
        }

        if (!AccountPasswordHasher.IsPasswordValid(account.Password, password))
        {
            return null;
        }

        if (!AccountPasswordHasher.IsCurrentPasswordFormat(account.Password))
        {
            var upgradedPassword = AccountPasswordHasher.HashPlainTextPasswordForStorage(password);
            if (upgradedPassword == null)
            {
                return null;
            }

            account.Password = upgradedPassword;
        }

        account.Ticket = Guid.NewGuid().ToString();
        account.LastAccess = DateTime.Now;
        account.IpAddress = ipAddress;

        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public static ClaimsPrincipal CreatePrincipal(Account account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
            new(ClaimTypes.Name, account.Name),
            new("Permission", account.Permission.ToString())
        };

        if (!string.IsNullOrWhiteSpace(account.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, account.Email));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static AuthenticationProperties CreateAuthenticationProperties(bool rememberMe)
    {
        return new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
        };
    }
}
