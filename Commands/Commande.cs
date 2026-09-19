using CertShell.Config;
using CertShell.Platform;

namespace CertShell.Commands;

public static class Commande
{
    private static string MountPoint => AppConfig.Instance.MountPoint;
    private static string ImgPath    => AppConfig.Instance.ImgPath;

    public static void LsCommande(string directory)
    {
        try
        {
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

    public static void RemoveMp4FileEncrypt()
    {
        try
        {
            if (!Directory.Exists(MountPoint)) return;

            foreach (var file in Directory.GetFiles(MountPoint, "*.mp4"))
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

    public static void MountImg() => PlatformHelper.MountImg(ImgPath, MountPoint);

    public static void RemoveCache()
    {
        if (File.Exists("Cache/file_list.txt")) File.Delete("Cache/file_list.txt");
        if (File.Exists("Cache/key_log_enter")) File.Delete("Cache/key_log_enter");
        if (File.Exists("Cache/key_log_tab"))   File.Delete("Cache/key_log_tab");
    }

    public static void ClearCommande() => Console.Clear();

    public static void RemoveObject(string inputDirectory)
    {
        PlatformHelper.DeleteFile(Path.Combine(MountPoint, inputDirectory));
    }

    public static void AddObject(string inputDirectory)
    {
        PlatformHelper.CopyFileToDirectory(inputDirectory, MountPoint);
        Console.WriteLine();
    }

    public static string CheckLaunchCertguard() =>
        PlatformHelper.CheckProcessRunning("CertGuard");

    public static void LaunchCertguard(string cryptFile)
    {
        try
        {
            PlatformHelper.LaunchInTerminal("CertGuard", Path.Combine(MountPoint, cryptFile));
            WaitForCertguardCycle();
            DefiningExtensions.Main(Path.Combine(MountPoint, cryptFile));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запуска CertGuard: {ex.Message}");
        }
    }

    public static void WaitForCertguardCycle()
    {
        var start = DateTime.Now;
        while (CheckLaunchCertguard() == "Not_launched")
        {
            if ((DateTime.Now - start).TotalSeconds > 10) return;
            Thread.Sleep(100);
        }
        while (CheckLaunchCertguard() == "Launched")
            Thread.Sleep(100);
    }

    public static void ExitToProgram()
    {
        RemoveMp4FileEncrypt();
        RemoveCache();

        PlatformHelper.CheckFuser(MountPoint);
        PlatformHelper.Umount(ImgPath);

        Environment.Exit(0);
    }

    public static void LsDirectory()
    {
        try
        {
            Console.WriteLine();
            if (Directory.Exists(MountPoint))
                foreach (var dir in Directory.GetDirectories(MountPoint))
                    Console.WriteLine(Path.GetFileName(dir));
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

    public static void CallOfSystemCommands(string commandeLine)
    {
        switch (commandeLine)
        {
            case "ls":    LsCommande(MountPoint); break;
            case "clear": ClearCommande(); break;
            case "add":
                Console.Write("Введите исходную директрию - ");
                string? addPath = Console.ReadLine();
                if (!string.IsNullOrEmpty(addPath) && File.Exists(addPath))
                    AddObject(addPath);
                else
                    Console.WriteLine("Файл не найден");
                break;
            case "remove":
                Console.Write("Введите название файла для удаления - ");
                string? remFile = Console.ReadLine();
                if (!string.IsNullOrEmpty(remFile) && File.Exists(Path.Combine(MountPoint, remFile)))
                    RemoveObject(remFile);
                else
                    Console.WriteLine("Файл не найден");
                break;
            case "exit":  ExitToProgram(); break;
            case "mount": MountImg(); break;
            case "lsdir": LsDirectory(); break;
        }
    }
}
