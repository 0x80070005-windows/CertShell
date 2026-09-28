using CertShell.Config;
using CertShell.FileSelector;
using CertShell.Platform;
using CertShell.Security;

namespace CertShell.Commands;

public static class ChangeDirectory
{
    private static string MountPoint    => AppConfig.Instance.MountPoint;
    private static string ImgPath       => AppConfig.Instance.ImgPath;
    private static string CertGuardPath => AppConfig.Instance.CertGuardPath;
    private static string CacheDir      => PlatformHelper.CacheDir;

    private static string NameDirectoryFile => Path.Combine(CacheDir, "name_directory");
    private static string CdFlagFile        => Path.Combine(CacheDir, "cd");

    public static void ClearCommandeLine() => Console.Clear();

    private static string ReadCurrentDirectory()
    {
        try { return File.ReadAllText(NameDirectoryFile).Trim(); }
        catch { return ""; }
    }

    // ===================== cd =====================

    public static void MoveToDirectory()
    {
        Console.Write("Введите название директории: ");
        string? nameFolder = Console.ReadLine();
        if (string.IsNullOrEmpty(nameFolder)) return;

        try
        {
            string path = Path.Combine(MountPoint, nameFolder);
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Папка не найдена");
                return;
            }
            File.WriteAllText(NameDirectoryFile, nameFolder);
        }
        catch (UnauthorizedAccessException) { Console.WriteLine("Нет доступа к директории"); }
        catch (Exception ex) { Console.WriteLine($"Ошибка: {ex.Message}"); }
    }

    public static void LsCommande()
    {
        try
        {
            string nameDirectory = ReadCurrentDirectory();
            string directory = Path.Combine(MountPoint, nameDirectory);
            if (!Directory.Exists(directory)) return;

            foreach (var item in Directory.GetFileSystemEntries(directory))
                Console.WriteLine("\t" + Path.GetFileName(item));
            Console.WriteLine();
        }
        catch (UnauthorizedAccessException) { Console.WriteLine("Нет доступа к директории."); }
        catch (Exception ex) { Console.WriteLine($"Ошибка чтения: {ex.Message}"); }
    }

    // ===================== add / remove =====================

    public static void RemoveObject()
    {
        try
        {
            string nameDirectory = ReadCurrentDirectory();
            Console.Write("Введите название файла: ");
            string? nameFile = Console.ReadLine();
            if (string.IsNullOrEmpty(nameFile)) return;

            string path = Path.Combine(MountPoint, nameDirectory, nameFile);
            if (File.Exists(path))
                PlatformHelper.DeleteFile(path);
            else
                Console.WriteLine("Файл не найден");
        }
        catch (Exception ex) { Console.WriteLine($"Ошибка удаления: {ex.Message}"); }
    }

    public static void AddObject()
    {
        try
        {
            string nameDirectory = ReadCurrentDirectory();
            Console.Write("Введите директорию: ");
            string? inputDir = Console.ReadLine();
            if (string.IsNullOrEmpty(inputDir) || !File.Exists(inputDir))
            {
                Console.WriteLine("Файл не найден");
                return;
            }

            string destDirectory = Path.Combine(MountPoint, nameDirectory);
            PlatformHelper.CopyFileToDirectory(inputDir, destDirectory);
        }
        catch (Exception ex) { Console.WriteLine($"Ошибка копирования: {ex.Message}"); }
    }

    // ===================== Запуск CertGuard =====================

    public static void LaunchCertguard(string cryptFile)
    {
        try
        {
            string nameDirectory = ReadCurrentDirectory();
            string encPath = Path.Combine(MountPoint, nameDirectory, cryptFile);

            if (!File.Exists(encPath))
            {
                Console.WriteLine($"Файл не найден: {encPath}");
                return;
            }

            string decPath = encPath.EndsWith(".enc", StringComparison.Ordinal)
                ? encPath[..^4]
                : encPath;

            if (File.Exists(decPath))
            {
                try { File.Delete(decPath); } catch { }
            }

            AuditLog.Info($"open file={cryptFile} dir={nameDirectory}");

            bool started = PlatformHelper.LaunchInTerminal(CertGuardPath, encPath);
            if (!started)
            {
                Console.WriteLine("Не удалось запустить CertGuard.");
                return;
            }

            if (!PlatformHelper.WaitForFileStable(decPath, 600))
            {
                Console.WriteLine($"Файл {decPath} не появился. CertGuard не завершился?");
                AuditLog.Warn($"decrypt_timeout file={cryptFile}");
                return;
            }

            DefiningExtensions.OpenFile(decPath);
        }
        catch (Exception ex)
        {
            AuditLog.Error($"launch_certguard_failed: {ex.Message}");
            Console.WriteLine($"Ошибка запуска CertGuard: {ex.Message}");
        }
    }

    // ===================== Cleanup =====================

    public static void RemoveMp4FileDecrypt()
    {
        try
        {
            string nameDirectory = ReadCurrentDirectory();
            string directory = Path.Combine(MountPoint, nameDirectory);
            if (!Directory.Exists(directory)) return;

            foreach (var file in Directory.GetFiles(directory, "*.mp4"))
                try { File.Delete(file); } catch { }
        }
        catch { }
    }

    public static void RemoveCache()
    {
        TryDelete(NameDirectoryFile);
        TryDelete(CdFlagFile);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    public static void ExitToProgram()
    {
        AuditLog.Info("session_exit");
        RemoveMp4FileDecrypt();
        RemoveCache();
        Commands.Commande.RemoveCache();

        PlatformHelper.CheckFuser(MountPoint);
        PlatformHelper.Umount(ImgPath);

        Environment.Exit(0);
    }

    // ===================== Диспетчер =====================

    public static void CallOfSystemCommands(string commandeLine)
    {
        switch (commandeLine)
        {
            case "ls":    LsCommande(); break;
            case "clear": ClearCommandeLine(); break;
            case "add":   AddObject(); break;
            case "remove":RemoveObject(); break;
            case "exit":  ExitToProgram(); break;
            case "open":  CallOfCertguardForChangeDirectory.OpenSelected(); break;
        }
    }
}
