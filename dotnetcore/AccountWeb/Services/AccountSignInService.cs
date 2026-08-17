using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Database;
using Database.Sqlite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace AccountWeb.Services;

public sealed class AccountSignInService
{
    private const int RequiredAccountLevel = 4;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 600_000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

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

        var passwordMd5Hash = ComputeMd5Hash(password);
        if (passwordMd5Hash == null || !IsPasswordValid(account.Password, passwordMd5Hash))
        {
            return null;
        }

        if (!IsCurrentPasswordFormat(account.Password))
        {
            var upgraded = HashPassword(passwordMd5Hash);
            account.Password = $"{upgraded.Hash}.{upgraded.Salt}";
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

    private static bool IsPasswordValid(string storedPassword, string passwordMd5Hash)
    {
        var parts = storedPassword.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            return VerifyPassword(passwordMd5Hash, parts[0], parts[1]);
        }

        return storedPassword == ComputeSha256Hash(passwordMd5Hash);
    }

    private static bool IsCurrentPasswordFormat(string storedPassword)
    {
        return storedPassword.Split('.', StringSplitOptions.RemoveEmptyEntries).Length == 2;
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        return (Convert.ToHexString(hash), Convert.ToHexString(salt));
    }

    private static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        byte[] salt;
        byte[] hash;

        try
        {
            salt = Convert.FromHexString(storedSalt);
            hash = Convert.FromHexString(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] newHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(hash, newHash);
    }

    private static string? ComputeMd5Hash(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        byte[] hash = MD5.HashData(Encoding.ASCII.GetBytes(value));
        var stringBuilder = new StringBuilder();

        for (int index = 0; index < hash.Length; index++)
        {
            stringBuilder.Append(hash[index].ToString("x2"));
        }

        return stringBuilder.ToString();
    }

    private static string ComputeSha256Hash(string input)
    {
        byte[] data = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var stringBuilder = new StringBuilder();

        for (int index = 0; index < data.Length; index++)
        {
            stringBuilder.Append(data[index].ToString("x2"));
        }

        return stringBuilder.ToString();
    }
}
