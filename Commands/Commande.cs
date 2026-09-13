using CertShell.Config;
using System.Diagnostics;

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
            Console.WriteLine("Не удалось прочитать директорию (нужны права root?)");
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

    public static void MountImg()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"sudo mount '{ImgPath}' '{MountPoint}'" },
                UseShellExecute = false
            })?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка монтирования: {ex.Message}");
        }
    }

    public static void RemoveCache()
    {
        if (File.Exists("Cache/file_list.txt")) File.Delete("Cache/file_list.txt");
        if (File.Exists("Cache/key_log_enter")) File.Delete("Cache/key_log_enter");
        if (File.Exists("Cache/key_log_tab"))   File.Delete("Cache/key_log_tab");
    }

    public static void ClearCommande() => Console.Clear();

    public static void RemoveObject(string inputDirectory)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"sudo rm '{Path.Combine(MountPoint, inputDirectory)}'" },
                UseShellExecute = false
            })?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка удаления: {ex.Message}");
        }
    }

    public static void AddObject(string inputDirectory)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"sudo cp '{inputDirectory}' '{MountPoint}'" },
                UseShellExecute = false
            })?.WaitForExit();
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка копирования: {ex.Message}");
        }
    }

    public static string CheckLaunchCertguard()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "pgrep",
                ArgumentList = { "-fl", "CertGuard" },
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            if (proc == null) return "Not_launched";
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return string.IsNullOrWhiteSpace(output) ? "Not_launched" : "Launched";
        }
        catch
        {
            return "Not_launched";
        }
    }

    public static void LaunchCertguard(string cryptFile)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "gnome-terminal",
                ArgumentList = { "--", "CertGuard", Path.Combine(MountPoint, cryptFile) },
                UseShellExecute = false
            });

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

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"fuser -v '{MountPoint}'" },
                UseShellExecute = false
            })?.WaitForExit();
        }
        catch { }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"sudo umount '{ImgPath}'" },
                UseShellExecute = false
            })?.WaitForExit();
        }
        catch { }

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
            Console.WriteLine("Не удалось прочитать директорию (нужны права root?)");
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
