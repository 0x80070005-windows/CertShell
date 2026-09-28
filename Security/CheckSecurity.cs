using CertShell.Config;
using CertShell.Platform;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertShell.Security;

public static class CheckSecurity
{
    private const int MaxLoginAttempts = 5;

    private static string CertPath   => AppConfig.Instance.CertPath;
    private static string InfoDir    => PlatformHelper.DataDir;
    private static string CertPinFile => Path.Combine(InfoDir, "cert_pin");
    private static string LoginFile  => Path.Combine(InfoDir, "login");
    private static string PassFile   => Path.Combine(InfoDir, "password");

    public static void Init()
    {
        try
        {
            CheckCertificate();
            LoginUser();
            AuditLog.Info("session_start");
        }
        catch (Exception ex)
        {
            AuditLog.Error($"init_failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    // ===================== Certificate pinning =====================

    private static void CheckCertificate()
    {
        if (!File.Exists(CertPath))
        {
            AuditLog.Error($"cert_missing path={CertPath}");
            Console.WriteLine("Сертификат не найден. Вставь флешку.");
            Environment.Exit(1);
        }

        // В .NET 8 используем конструктор, а не X509CertificateLoader (.NET 9+).
        X509Certificate2 cert;
        try
        {
            cert = new X509Certificate2(CertPath);
        }
        catch (CryptographicException ex)
        {
            AuditLog.Error($"cert_load_failed: {ex.Message}");
            Console.WriteLine($"Не удалось прочитать сертификат: {ex.Message}");
            Environment.Exit(1);
            return;
        }

        using (cert)
        {
            DateTime now = DateTime.Now;
            if (now < cert.NotBefore || now > cert.NotAfter)
            {
                AuditLog.Error("cert_expired");
                Console.WriteLine("Срок действия сертификата истёк.");
                Environment.Exit(1);
            }

            // SHA-256 от всего DER-сертификата — самый надёжный «отпечаток».
            string currentPin = Convert.ToHexString(SHA256.HashData(cert.RawData));

            if (!File.Exists(CertPinFile))
            {
                File.WriteAllText(CertPinFile, currentPin);
                TryChmod600(CertPinFile);
                AuditLog.Info("cert_pin_initialized");
                return;
            }

            string expectedPin = File.ReadAllText(CertPinFile).Trim();

            if (!FixedTimeHexEquals(currentPin, expectedPin))
            {
                AuditLog.Error("cert_pin_mismatch");
                Console.WriteLine("Сертификат не совпадает с закреплённым. Доступ запрещён.");
                Environment.Exit(1);
            }
        }
    }

    // ===================== Логин =====================

    private static void LoginUser()
    {
        if (!File.Exists(LoginFile) || !File.Exists(PassFile))
        {
            AuditLog.Error("credentials_missing");
            Console.WriteLine("Учётные данные отсутствуют. Запусти с --setup.");
            Environment.Exit(1);
        }

        string storedLogin = File.ReadAllText(LoginFile).Trim();
        string storedPass  = File.ReadAllText(PassFile).Trim();

        for (int attempt = 1; attempt <= MaxLoginAttempts; attempt++)
        {
            Console.Write("Введите логин - ");
            string login = Console.ReadLine() ?? "";

            Console.Write("Введите пароль - ");
            string password = ReadPassword();

            bool loginOk = VerifyCredential(login, storedLogin);
            bool passOk  = PasswordHasher.Verify(password, storedPass);

            if (loginOk && passOk)
            {
                AuditLog.Info($"login_ok user={ShortHash(storedLogin)}");
                return;
            }

            AuditLog.Warn($"login_fail attempt={attempt}");
            Console.WriteLine("Неверный логин или пароль.");

            if (attempt < MaxLoginAttempts)
            {
                int delay = 1 << (attempt - 1); // 1, 2, 4, 8
                Console.WriteLine($"Подожди {delay} сек...");
                Thread.Sleep(delay * 1000);
            }
        }

        AuditLog.Error("login_lockout");
        Console.WriteLine("Слишком много неудачных попыток. Выход.");
        Environment.Exit(1);
    }

    private static bool VerifyCredential(string input, string stored)
    {
        if (PasswordHasher.IsHashed(stored))
            return PasswordHasher.Verify(input, stored);

        // Legacy: SHA-512 hex
        byte[] data = Encoding.UTF8.GetBytes(input);
        byte[] h = SHA512.HashData(data);
        string hex = Convert.ToHexString(h).ToLowerInvariant();
        return FixedTimeHexEquals(hex, stored);
    }

    // ===================== Утилиты =====================

    private static bool FixedTimeHexEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(a),
                Convert.FromHexString(b));
        }
        catch { return false; }
    }

    private static string ShortHash(string s) => s.Length <= 8 ? s : s[..8];

    private static void TryChmod600(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        try { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
        catch { }
    }

    private static string ReadPassword()
    {
        var sb = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo key;
            try { key = Console.ReadKey(intercept: true); }
            catch { return sb.ToString(); }

            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return sb.ToString(); }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); }
            }
            else
            {
                sb.Append(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}
