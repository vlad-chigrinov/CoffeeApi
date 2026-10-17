using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace CoffeeApi.Api.Services;

public record GeneratedApiKey(string Key, string Hash, string Prefix);

public static class ApiKeyService
{
    private const string KeyPrefix = "cof_";
    private const int PrefixLength = 8;

    public static GeneratedApiKey Generate()
    {
        var key = KeyPrefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return new GeneratedApiKey(key, Hash(key), key[..PrefixLength]);
    }

    /// <summary>SHA-256 of the key as lowercase hex (64 chars).</summary>
    public static string Hash(string key) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}
