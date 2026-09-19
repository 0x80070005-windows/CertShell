using CertShell.Config;
using CertShell.FileSelector;
using CertShell.Platform;

namespace CertShell.Commands;

public static class ChangeDirectory
{
    private static string MountPoint => AppConfig.Instance.MountPoint;
    private static string ImgPath    => AppConfig.Instance.ImgPath;

    public static void ClearCommandeLine() => Console.Clear();

    public static void MoveToDirectory()
    {
        Console.Write("Введите название директоррии: ");
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
            File.WriteAllText("Cache/name_directory", nameFolder);
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Нет доступа к директории");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    public static void LsCommande()
    {
        try
        {
            string nameDirectory = File.ReadAllText("Cache/name_directory").Trim();
            string directory = Path.Combine(MountPoint, nameDirectory);
            if (!Directory.Exists(directory)) return;

            foreach (var item in Directory.GetFileSystemEntries(directory))
                Console.WriteLine("\t" + Path.GetFileName(item));
            Console.WriteLine();
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Не удалось прочитать директорию (нужны права администратора/root?)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка чтения: {ex.Message}");
        }
    }

    public static void RemoveMp4FileDecrypt()
    {
        try
        {
            string nameDirectory = File.ReadAllText("Cache/name_directory").Trim();
            string directory = Path.Combine(MountPoint, nameDirectory);
            if (!Directory.Exists(directory)) return;

            foreach (var file in Directory.GetFiles(directory, "*.mp4"))
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"Удален файл: {file}");
                }
                catch { }
            }
        }
        catch { }
    }

    public static void RemoveCache()
    {
        if (File.Exists("Cache/name_directory")) File.Delete("Cache/name_directory");
        if (File.Exists("Cache/cd"))             File.Delete("Cache/cd");
    }

    public static void RemoveObject()
    {
        try
        {
            string nameDirectory = File.ReadAllText("Cache/name_directory").Trim();
            Console.Write("Введите название файла: ");
            string? nameFile = Console.ReadLine();
            if (string.IsNullOrEmpty(nameFile)) return;

            string path = Path.Combine(MountPoint, nameDirectory, nameFile);
            if (File.Exists(path))
                PlatformHelper.DeleteFile(path);
            else
                Console.WriteLine("Файл не найден");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка удаления: {ex.Message}");
        }
    }

    public static void AddObject()
    {
        try
        {
            string nameDirectory = File.ReadAllText("Cache/name_directory").Trim();
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
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка копирования: {ex.Message}");
        }
    }

    public static string CheckLaunchCertguard() => Commande.CheckLaunchCertguard();

    public static void LaunchCertguard(string cryptFile)
    {
        try
        {
            string nameDirectory = File.ReadAllText("Cache/name_directory").Trim();
            string directory = Path.Combine(MountPoint, nameDirectory, cryptFile);

            if (!File.Exists(directory))
                return;

            PlatformHelper.LaunchInTerminal("CertGuard", directory);
            Commande.WaitForCertguardCycle();

            nameDirectory = File.ReadAllText("Cache/name_directory").Trim();
            string directoryToSelectedFile = Path.Combine(MountPoint, nameDirectory, cryptFile);
            DefiningExtensions.Main(directoryToSelectedFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запуска CertGuard: {ex.Message}");
        }
    }

    public static void ExitToProgram()
    {
        RemoveMp4FileDecrypt();
        RemoveCache();
        Commande.RemoveCache();

        PlatformHelper.CheckFuser(MountPoint);
        PlatformHelper.Umount(ImgPath);

        Environment.Exit(0);
    }

    public static void CallOfSystemCommands(string commandeLine)
    {
        switch (commandeLine)
        {
            case "ls":    LsCommande(); break;
            case "clear": ClearCommandeLine(); break;
            case "add":   AddObject(); break;
            case "remove":RemoveObject(); break;
            case "exit":  ExitToProgram(); break;
            case "open":  CallOfCertguardForChangeDirectory.Main(); break;
        }
    }
}
