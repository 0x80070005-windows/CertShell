using CertShell.Config;
using CertShell.Platform;
using System.Text;

namespace CertShell.Security;

public static class SetupWizard
{
    private static string InfoDir => PlatformHelper.DataDir;

    public static void Run()
    {
        Console.Clear();
        Console.WriteLine("=== CertShell — первый запуск ===");
        Console.WriteLine();
        Console.WriteLine("CertShell использует сертификат X.509 как физический фактор");
        Console.WriteLine("для расшифровки файлов CertGuard.");
        Console.WriteLine();
        Console.WriteLine("Перед продолжением убедись, что:");
        Console.WriteLine("  1. Сертификат создан (openssl или CertificateGenerator CertGuard)");
        Console.WriteLine("  2. Файл сертификата лежит на съёмном носителе (флешке)");
        Console.WriteLine("  3. Флешка подключена и доступна");
        Console.WriteLine();

        string certPath      = AskCertPath();
        string imgPath       = AskImgPath();
        string mountPoint    = AskMountPoint();
        string certGuardPath = AskCertGuardPath();

        var config = new AppConfig
        {
            CertPath      = certPath,
            ImgPath       = imgPath,
            MountPoint    = mountPoint,
            CertGuardPath = certGuardPath,
        };

        AppConfig.Save(config);
        PlatformHelper.EnsureDataDirs();

        Console.WriteLine();
        Console.WriteLine("Конфигурация сохранена:");
        Console.WriteLine($"  Cert:       {certPath}");
        Console.WriteLine($"  Img:        {imgPath}");
        Console.WriteLine($"  MountPoint: {mountPoint}");
        Console.WriteLine($"  CertGuard:  {certGuardPath}");

        AskCredentials();

        Console.WriteLine();
        Console.WriteLine("Настройка завершена. Нажми Enter чтобы продолжить.");
        Console.ReadLine();
        Console.Clear();
    }

    // ===================== Учётные данные =====================

    private static void AskCredentials()
    {
        Console.WriteLine();
        Console.WriteLine("=== Учётные данные ===");
        Console.WriteLine("Придумай логин и пароль для входа в CertShell.");
        Console.WriteLine("Пароль хешируется через Argon2id (64 MiB × 3).");
        Console.WriteLine("Используй длинный пароль — 12+ символов.");
        Console.WriteLine();

        string login    = AskNonEmpty("Логин: ");
        string password = AskPassword("Пароль: ");
        string confirm  = AskPassword("Повтори пароль: ");

        if (password != confirm)
        {
            Console.WriteLine("Пароли не совпадают. Попробуй снова.");
            AskCredentials();
            return;
        }

        if (password.Length < 8)
        {
            Console.WriteLine("Слишком короткий пароль. Минимум 8 символов.");
            AskCredentials();
            return;
        }

        Directory.CreateDirectory(InfoDir);

        File.WriteAllText(Path.Combine(InfoDir, "login"),    PasswordHasher.Hash(login));
        File.WriteAllText(Path.Combine(InfoDir, "password"), PasswordHasher.Hash(password));

        if (!PlatformHelper.IsWindows)
        {
            foreach (var f in new[] { "login", "password" })
            {
                try { File.SetUnixFileMode(Path.Combine(InfoDir, f), UnixFileMode.UserRead | UnixFileMode.UserWrite); }
                catch { }
            }
        }

        AuditLog.Info("credentials_created");
        Console.WriteLine();
        Console.WriteLine("Учётные данные сохранены.");
    }

    // ===================== Запросы =====================

    private static string AskNonEmpty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? value = Console.ReadLine();
            if (!string.IsNullOrEmpty(value)) return value;
        }
    }

    private static string AskPassword(string prompt)
    {
        Console.Write(prompt);
        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
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

    private static string AskCertPath()
    {
        while (true)
        {
            Console.Write("Путь к папке с сертификатом (там где ca.crt или certificate.cer): ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            input = ExpandHome(input);

            foreach (var name in new[] { "ca.crt", "certificate.cer" })
            {
                string full = Path.Combine(input, name);
                if (File.Exists(full)) return full;
            }

            // Возможно, ввели полный путь к файлу
            if (File.Exists(input)) return input;

            Console.WriteLine("  Файл не найден. Попробуй ещё раз.");
            Console.WriteLine();
        }
    }

    private static string AskImgPath()
	{
	    string hint = PlatformHelper.IsWindows
		? "Путь к файлу образа виртуального диска (.img / .vhd / .vhdx): "
		: "Путь к образу виртуального диска (.img): ";

	    while (true)
	    {
		Console.Write(hint);
		string? raw = Console.ReadLine();
		if (raw == null) continue;

		string input = raw.Trim().Trim('"', '\'');

		if (string.IsNullOrEmpty(input))
		{
		    Console.WriteLine("  Пусто. Попробуй ещё раз.");
		    continue;
		}

		string expanded = ExpandHome(input);

		Console.WriteLine($"  Введено:      {raw}");
		Console.WriteLine($"  Обработано:   {expanded}");

		if (File.Exists(expanded))
		{
		    Console.WriteLine("  ✓ Файл найден.");
		    return expanded;
		}

		// Диагностика
		if (!Path.IsPathRooted(expanded))
		    Console.WriteLine("  ⚠ Путь относительный. Используй абсолютный (/home/...) или ~/...");

		string? dir = Path.GetDirectoryName(expanded);
		if (string.IsNullOrEmpty(dir))
		{
		    Console.WriteLine("  ⚠ Не удалось определить директорию.");
		}
		else if (!Directory.Exists(dir))
		{
		    Console.WriteLine($"  ⚠ Директория не существует: {dir}");
		}
		else
		{
		    Console.WriteLine($"  Директория есть: {dir}");
		    Console.WriteLine("  Содержимое (первые 10):");

		    try
		    {
		        int shown = 0;
		        foreach (var f in Directory.GetFileSystemEntries(dir))
		        {
		            Console.WriteLine($"    {Path.GetFileName(f)}");
		            if (++shown >= 10) break;
		        }
		        if (shown == 0) Console.WriteLine("    (пусто)");
		    }
		    catch (Exception ex)
		    {
		        Console.WriteLine($"    (ошибка чтения: {ex.Message})");
		    }
		}

		Console.WriteLine("  Попробуй ещё раз.");
		Console.WriteLine();
	    }
}

    private static string AskMountPoint()
    {
        string def = PlatformHelper.DefaultMountPoint;

        Console.WriteLine();
        if (PlatformHelper.IsWindows)
        {
            Console.WriteLine("На Windows .img-образ нужно монтировать вручную");
            Console.WriteLine("(например, через WSL, imdisk или OSFMount).");
        }

        Console.Write($"Точка монтирования виртуального диска (Enter = {def}): ");
        string? input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input)) return def;
        return ExpandHome(input);
    }

    private static string AskCertGuardPath()
    {
        Console.WriteLine();
        Console.Write("Путь к бинарю CertGuard (Enter = искать в PATH): ");
        string? input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
            return "CertGuard";

        input = ExpandHome(input);

        if (!File.Exists(input))
        {
            Console.WriteLine($"  Файл {input} не найден — буду искать 'CertGuard' в PATH.");
            return "CertGuard";
        }

        return input;
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
}
