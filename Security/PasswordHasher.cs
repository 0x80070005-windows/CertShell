using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace CertShell.Security;

/// <summary>
/// Хранение и проверка паролей через Argon2id с солью.
/// Формат: argon2id$&lt;salt_hex&gt;$&lt;hash_hex&gt;
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemoryKiB = 65536;   // 64 MiB
    private const int Iterations = 3;
    private const int Parallelism = 4;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Compute(password, salt);
        return $"argon2id${Convert.ToHexString(salt)}${Convert.ToHexString(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;

        string[] parts = stored.Split('$');
        if (parts.Length != 3 || parts[0] != "argon2id") return false;

        try
        {
            byte[] salt = Convert.FromHexString(parts[1]);
            byte[] expected = Convert.FromHexString(parts[2]);
            byte[] actual = Compute(password, salt);

            if (expected.Length != actual.Length) return false;
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch { return false; }
    }

    /// <summary>Проверяет, что строка уже в формате argon2id$salt$hash.</summary>
    public static bool IsHashed(string s) =>
        !string.IsNullOrEmpty(s) && s.StartsWith("argon2id$", StringComparison.Ordinal);

    private static byte[] Compute(string password, byte[] salt)
    {
        byte[] pwd = Encoding.UTF8.GetBytes(password);
        try
        {
            using var argon2 = new Argon2id(pwd)
            {
                Salt = salt,
                DegreeOfParallelism = Parallelism,
                MemorySize = MemoryKiB,
                Iterations = Iterations
            };
            return argon2.GetBytes(HashSize);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pwd);
        }
    }
}
