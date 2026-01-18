using System.Security.Cryptography;
using System.Text;

namespace WhereAreThey.Services;

/// <summary>
/// Provides hashing utilities for privacy-preserving data handling.
/// </summary>
public static class HashUtils
{
    /// <summary>
    /// Computes a SHA256 hash of an email address for privacy-preserving verification checks.
    /// </summary>
    /// <param name="email">The email address to hash.</param>
    /// <returns>A hex string representation of the SHA256 hash.</returns>
    public static string ComputeHash(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalizedEmail);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
