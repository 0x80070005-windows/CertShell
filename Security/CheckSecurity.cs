using CertShell.Config;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertShell.Security;

public static class CheckSecurity
{
    private static string CertPath => AppConfig.Instance.CertPath;

    private static string InfoDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local", "share", "CertShell");

    public static void Init()
    {
        try
        {
            CheckCertificate();
            LoginUser();
        }
        catch
        {
            Environment.Exit(0);
        }
    }

    private static void CheckCertificate()
    {
        if (!File.Exists(CertPath))
            Environment.Exit(0);

        using var cert = new X509Certificate2(CertPath);

        string subject = NormalizeRfc4514(cert.Subject);
        string issuer  = NormalizeRfc4514(cert.Issuer);
        string serial  = GetSerialDecimal(cert);
        string sigAlgo = SignatureHashName(cert.SignatureAlgorithm);

        string hashSubject = Hash(subject);
        string hashIssuer  = Hash(issuer);
        string hashSerial  = Hash(serial);
        string hashSigAlgo = Hash(sigAlgo);

        Directory.CreateDirectory(InfoDir);

        string p1 = Path.Combine(InfoDir, "1");
        string p2 = Path.Combine(InfoDir, "2");
        string p3 = Path.Combine(InfoDir, "3");
        string p4 = Path.Combine(InfoDir, "4");

        if (!File.Exists(p1) || !File.Exists(p2) || !File.Exists(p3) || !File.Exists(p4))
        {
            File.WriteAllText(p1, hashSubject);
            File.WriteAllText(p2, hashIssuer);
            File.WriteAllText(p3, hashSerial);
            File.WriteAllText(p4, hashSigAlgo);
            return;
        }

        string expectedSubject = File.ReadAllText(p1).Trim();
        string expectedIssuer  = File.ReadAllText(p2).Trim();
        string expectedSerial  = File.ReadAllText(p3).Trim();
        string expectedSigAlgo = File.ReadAllText(p4).Trim();

        if (hashSubject != expectedSubject ||
            hashIssuer  != expectedIssuer  ||
            hashSerial  != expectedSerial  ||
            hashSigAlgo != expectedSigAlgo)
        {
            Environment.Exit(0);
        }
    }

    private static void LoginUser()
    {
        string loginFile    = File.ReadAllText(Path.Combine(InfoDir, "login")).Trim();
        string passwordFile = File.ReadAllText(Path.Combine(InfoDir, "password")).Trim();

        Console.Write("Введите логин - ");
        string? login = Console.ReadLine() ?? "";
        string loginHash = Hash(login);

        Console.Write("Введите пароль - ");
        string password = ReadPassword();
        string passwordHash = Hash(password);

        if (loginHash != loginFile || passwordHash != passwordFile)
            Environment.Exit(0);
    }

    private static string ReadPassword()
    {
        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return sb.ToString();
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                {
                    sb.Length--;
                    Console.Write("\b \b");
                }
            }
            else
            {
                sb.Append(key.KeyChar);
                Console.Write('*');
            }
        }
    }

    private static string Hash(string s)
    {
        byte[] data = Encoding.UTF8.GetBytes(s);
        byte[] h = SHA512.HashData(data);
        return Convert.ToHexString(h).ToLowerInvariant();
    }

    private static string NormalizeRfc4514(string dn) => dn.Replace(", ", ",");

    private static string GetSerialDecimal(X509Certificate2 cert)
    {
        try
        {
            string hex = cert.SerialNumber;
            if (string.IsNullOrEmpty(hex)) return "0";
            var bi = BigInteger.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return bi.ToString();
        }
        catch
        {
            return cert.SerialNumber ?? "0";
        }
    }

    private static string SignatureHashName(Oid oid) => oid.Value switch
    {
        "1.2.840.113549.1.1.4"  => "sha1",
        "1.2.840.113549.1.1.5"  => "sha1",
        "1.2.840.113549.1.1.11" => "sha256",
        "1.2.840.113549.1.1.12" => "sha384",
        "1.2.840.113549.1.1.13" => "sha512",
        "1.2.840.10045.4.1"     => "sha1",
        "1.2.840.10045.4.3.2"   => "sha256",
        "1.2.840.10045.4.3.3"   => "sha384",
        "1.2.840.10045.4.3.4"   => "sha512",
        _ => oid.FriendlyName ?? oid.Value ?? ""
    };
}
