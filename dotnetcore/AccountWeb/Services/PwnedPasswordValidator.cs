using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace AccountWeb.Services;

public static class PwnedPasswordValidator
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static async Task<PwnedPasswordCheckResult> CheckAsync(
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(password))
        {
            return PwnedPasswordCheckResult.NotPwned;
        }

        var sha1Hash = ComputeSha1Hash(password);
        var prefix = sha1Hash[..5];
        var suffix = sha1Hash[5..];

        HttpResponseMessage response;
        try
        {
            response = await HttpClient.GetAsync($"range/{prefix}", cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PwnedPasswordCheckResult.Unavailable;
        }
        catch (HttpRequestException)
        {
            return PwnedPasswordCheckResult.Unavailable;
        }

        using (response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return PwnedPasswordCheckResult.Unavailable;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var reader = new StringReader(responseBody);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                var parts = line.Split(':', 2);
                if (parts.Length != 2 || !parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return int.TryParse(parts[1], out var breachCount)
                    ? PwnedPasswordCheckResult.Pwned(breachCount)
                    : PwnedPasswordCheckResult.Pwned(1);
            }
        }

        return PwnedPasswordCheckResult.NotPwned;
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.pwnedpasswords.com/"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("InfantryOnlineAccountWeb", "1.0"));
        httpClient.DefaultRequestHeaders.Add("Add-Padding", "true");

        return httpClient;
    }

    private static string ComputeSha1Hash(string value)
    {
        byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }
}

public sealed record PwnedPasswordCheckResult(bool IsPwned, int BreachCount, bool IsUnavailable)
{
    public static PwnedPasswordCheckResult NotPwned { get; } = new(false, 0, false);

    public static PwnedPasswordCheckResult Unavailable { get; } = new(false, 0, true);

    public static PwnedPasswordCheckResult Pwned(int breachCount)
    {
        return new PwnedPasswordCheckResult(true, breachCount, false);
    }
}
