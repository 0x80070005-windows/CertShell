using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CertShell.Platform;

public static class PlatformHelper
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsLinux   => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsMacOS   => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Config/data directory:
    ///   Windows → %APPDATA%\CertShell
    ///   Linux/Mac → ~/.local/share/CertShell
    /// </summary>
    public static string DataDir => IsWindows
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CertShell")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "CertShell");

    /// <summary>Default mount point suggestion shown during setup.</summary>
    public static string DefaultMountPoint => IsWindows
        ? @"C:\CertShellMount\"
        : "/mnt/virtual_disk/";

    // ── File operations ─────────────────────────────────────────────────────

    /// <summary>
    /// Delete a file.
    /// Linux: sudo rm (mount point may need root).
    /// Windows: File.Delete directly.
    /// </summary>
    public static void DeleteFile(string path)
    {
        if (IsWindows)
        {
            try
            {
                File.Delete(path);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Нет прав для удаления (запусти от имени администратора?)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления: {ex.Message}");
            }
        }
        else
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    ArgumentList = { "-c", $"sudo rm '{path}'" },
                    UseShellExecute = false
                })?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Copy a file into a destination directory.
    /// Linux: sudo cp (mount point may need root).
    /// Windows: File.Copy directly.
    /// </summary>
    public static void CopyFileToDirectory(string sourcePath, string destDirectory)
    {
        if (IsWindows)
        {
            try
            {
                string destPath = Path.Combine(destDirectory, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, destPath, overwrite: true);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Нет прав для копирования (запусти от имени администратора?)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка копирования: {ex.Message}");
            }
        }
        else
        {
            try
            {
                string dest = destDirectory.TrimEnd('/') + "/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    ArgumentList = { "-c", $"sudo cp '{sourcePath}' '{dest}'" },
                    UseShellExecute = false
                })?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка копирования: {ex.Message}");
            }
        }
    }

    // ── Disk / mount ────────────────────────────────────────────────────────

    /// <summary>
    /// Mount a .img file. Linux only — on Windows prints a hint.
    /// </summary>
    public static void MountImg(string imgPath, string mountPoint)
    {
        if (IsWindows)
        {
            Console.WriteLine("Автоматическое монтирование .img не поддерживается на Windows.");
            Console.WriteLine($"Смонтируй образ вручную (через WSL / imdisk / OSFMount)");
            Console.WriteLine($"и убедись, что он доступен по пути: {mountPoint}");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"sudo mount '{imgPath}' '{mountPoint}'" },
                UseShellExecute = false
            })?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка монтирования: {ex.Message}");
        }
    }

    /// <summary>Unmount the image. Linux only — no-op on Windows.</summary>
    public static void Umount(string imgPath)
    {
        if (IsWindows) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"sudo umount '{imgPath}'" },
                UseShellExecute = false
            })?.WaitForExit();
        }
        catch { }
    }

    /// <summary>Check open file handles. Linux: fuser -v. Windows: no-op.</summary>
    public static void CheckFuser(string mountPoint)
    {
        if (IsWindows) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"fuser -v '{mountPoint}'" },
                UseShellExecute = false
            })?.WaitForExit();
        }
        catch { }
    }

    // ── Process helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Check whether a named process is running.
    /// Linux: pgrep -fl. Windows: Process.GetProcessesByName.
    /// </summary>
    public static string CheckProcessRunning(string name)
    {
        if (IsWindows)
        {
            return Process.GetProcessesByName(name).Length > 0 ? "Launched" : "Not_launched";
        }

        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "pgrep",
                ArgumentList = { "-fl", name },
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

    /// <summary>
    /// Launch a program in a new terminal window.
    /// Windows: tries Windows Terminal (wt.exe), falls back to cmd /c start.
    /// Linux: gnome-terminal --.
    /// </summary>
    public static void LaunchInTerminal(string program, string argument)
    {
        if (IsWindows)
        {
            // Try Windows Terminal first
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-- \"{program}\" \"{argument}\"",
                    UseShellExecute = false
                });
                return;
            }
            catch { /* wt not installed */ }

            // Fall back to cmd /c start
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{program}\" \"{argument}\"",
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска терминала: {ex.Message}");
            }
        }
        else
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "gnome-terminal",
                    ArgumentList = { "--", program, argument },
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска терминала: {ex.Message}");
            }
        }
    }

    // ── File opening ─────────────────────────────────────────────────────────

    /// <summary>
    /// Open a file with the OS default application.
    /// Windows: ShellExecute (respects file associations).
    /// macOS:   open.
    /// Linux:   xdg-open.
    /// </summary>
    public static void OpenWithDefault(string path)
    {
        try
        {
            if (IsWindows)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            else if (IsMacOS)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    ArgumentList = { path },
                    UseShellExecute = false
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    ArgumentList = { path },
                    UseShellExecute = false
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка открытия файла: {ex.Message}");
        }
    }
}
