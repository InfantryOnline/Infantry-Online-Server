using System.Security.Cryptography;
using System.Text;

namespace AccountWeb.Services;

public static class AccountPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 600_000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public static string? HashPlainTextPasswordForStorage(string password)
    {
        var md5Hash = ComputeMd5Hash(password);
        if (md5Hash == null)
        {
            return null;
        }

        var upgraded = HashPassword(md5Hash);
        return $"{upgraded.Hash}.{upgraded.Salt}";
    }

    public static bool IsPasswordValid(string storedPassword, string password)
    {
        var passwordMd5Hash = ComputeMd5Hash(password);
        if (passwordMd5Hash == null)
        {
            return false;
        }

        var parts = storedPassword.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            return VerifyPassword(passwordMd5Hash, parts[0], parts[1]);
        }

        return storedPassword == ComputeSha256Hash(passwordMd5Hash);
    }

    public static bool IsCurrentPasswordFormat(string storedPassword)
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
