using CertShell.Config;
using System.Security.Cryptography;
using System.Text;

namespace CertShell.Security;

public static class SetupWizard
{
    private static string InfoDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local", "share", "CertShell");

    public static void Run()
    {
        Console.Clear();
        Console.WriteLine("=== CertShell — первый запуск ===");
        Console.WriteLine();
        Console.WriteLine("CertShell использует самоподписанный сертификат OpenSSL");
        Console.WriteLine("как физический фактор для расшифровки файлов CertGuard.");
        Console.WriteLine();
        Console.WriteLine("Перед продолжением убедись, что:");
        Console.WriteLine("  1. Ты создал сертификат командой:");
        Console.WriteLine();
        Console.WriteLine("     openssl req -x509 -newkey rsa:4096 -sha256 -days 3650 -nodes \\");
        Console.WriteLine("         -keyout ca.key -out ca.crt \\");
        Console.WriteLine("         -subj \"/C=RU/ST=Omsk/L=Cherlack/O=CertShell/CN=admin\"");
        Console.WriteLine();
        Console.WriteLine("  2. Файл ca.crt лежит на съёмном носителе (флешке)");
        Console.WriteLine("  3. Флешка подключена и доступна");
        Console.WriteLine();

        string certPath = AskCertPath();
        string imgPath  = AskImgPath();

        var config = new AppConfig
        {
            CertPath = certPath,
            ImgPath = imgPath,
            MountPoint = "/mnt/virtual_disk/"
        };

        AppConfig.Save(config);

        Console.WriteLine();
        Console.WriteLine("Конфигурация сохранена:");
        Console.WriteLine($"  Cert: {certPath}");
        Console.WriteLine($"  Img:  {imgPath}");

        AskCredentials();

        Console.WriteLine();
        Console.WriteLine("Настройка завершена. Нажми Enter чтобы продолжить.");
        Console.ReadLine();
        Console.Clear();
    }

    private static void AskCredentials()
    {
        Console.WriteLine();
        Console.WriteLine("=== Учётные данные ===");
        Console.WriteLine("Придумай логин и пароль для входа в CertShell.");
        Console.WriteLine("Они будут храниться в виде SHA-512 хешей.");
        Console.WriteLine();

        string login = AskNonEmpty("Логин: ");
        string password = AskPassword("Пароль: ");
        string confirm = AskPassword("Повтори пароль: ");

        if (password != confirm)
        {
            Console.WriteLine("Пароли не совпадают. Попробуй снова.");
            AskCredentials();
            return;
        }

        Directory.CreateDirectory(InfoDir);

        File.WriteAllText(Path.Combine(InfoDir, "login"), Hash(login));
        File.WriteAllText(Path.Combine(InfoDir, "password"), Hash(password));

        try
        {
            File.SetUnixFileMode(Path.Combine(InfoDir, "login"),
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.SetUnixFileMode(Path.Combine(InfoDir, "password"),
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch { }

        Console.WriteLine();
        Console.WriteLine("Учётные данные сохранены.");
    }

    private static string AskNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? value = Console.ReadLine();
            if (!string.IsNullOrEmpty(value))
                return value;
        }
    }

    private static string AskPassword(string prompt)
    {
        Console.Write(prompt);
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

    private static string AskCertPath()
    {
        while (true)
        {
            Console.Write("Путь к папке с сертификатом (там где ca.crt): ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            input = ExpandHome(input);
            string full = Path.Combine(input, "ca.crt");

            if (File.Exists(full))
                return full;

            Console.WriteLine($"  Файл {full} не найден. Попробуй ещё раз.");
            Console.WriteLine();
        }
    }

    private static string AskImgPath()
    {
        while (true)
        {
            Console.Write("Путь к образу виртуального диска (.img): ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            input = ExpandHome(input);

            if (File.Exists(input))
                return input;

            Console.WriteLine($"  Файл {input} не найден. Попробуй ещё раз.");
            Console.WriteLine();
        }
    }

    private static string ExpandHome(string path)
    {
        if (path.StartsWith("~/"))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path[2..]);
        }
        return path;
    }

    private static string Hash(string s)
    {
        byte[] data = Encoding.UTF8.GetBytes(s);
        byte[] h = SHA512.HashData(data);
        return Convert.ToHexString(h).ToLowerInvariant();
    }
}
