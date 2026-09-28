using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CertShell.Platform;

public static class PlatformHelper
{
    // ===================== Платформа =====================

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsLinux   => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsMacOS   => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    // ===================== Пути =====================

    /// <summary>
    /// Каталог данных:
    ///   Windows   → %APPDATA%\CertShell
    ///   Linux/Mac → ~/.local/share/CertShell
    /// </summary>
    public static string DataDir => IsWindows
        ? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CertShell")
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "CertShell");

    /// <summary>Каталог кэша внутри DataDir.</summary>
    public static string CacheDir => Path.Combine(DataDir, "Cache");

    /// <summary>Точка монтирования по умолчанию (подсказка в setup).</summary>
    public static string DefaultMountPoint => IsWindows
        ? @"C:\CertShellMount\"
        : "/mnt/virtual_disk/";

    /// <summary>
    /// Создаёт DataDir и CacheDir с правами 0700 на Linux/Mac.
    /// </summary>
    public static void EnsureDataDirs()
    {
        try { Directory.CreateDirectory(DataDir); } catch { }
        try { Directory.CreateDirectory(CacheDir); } catch { }

        if (IsWindows) return;

        TryChmod(DataDir,  UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        TryChmod(CacheDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    private static void TryChmod(string path, UnixFileMode mode)
    {
        if (IsWindows) return;
        try { File.SetUnixFileMode(path, mode); } catch { }
    }

    // ===================== Файлы =====================

    /// <summary>
    /// Удаляет файл. Linux — через rm (может понадобиться для mount point с root-правами).
    /// </summary>
    public static void DeleteFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (IsWindows)
        {
            try { File.Delete(path); }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Нет прав для удаления (запусти от имени администратора?)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления: {ex.Message}");
            }
            return;
        }

        try
        {
            string safe = path.Replace("'", "'\\''");
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"rm -f -- '{safe}'" },
                UseShellExecute = false
            };
            Process.Start(psi)?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка удаления: {ex.Message}");
        }
    }

    /// <summary>
    /// Копирует файл в директорию. Linux — через cp.
    /// </summary>
    public static void CopyFileToDirectory(string sourcePath, string destDirectory)
    {
        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destDirectory)) return;

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
            return;
        }

        try
        {
            string dest = destDirectory.TrimEnd('/') + "/";
            string safeSrc  = sourcePath.Replace("'", "'\\''");
            string safeDest = dest.Replace("'", "'\\''");

            var psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"cp -f -- '{safeSrc}' '{safeDest}'" },
                UseShellExecute = false
            };
            Process.Start(psi)?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка копирования: {ex.Message}");
        }
    }

    // ===================== Монтирование =====================

    /// <summary>
    /// Монтирует .img-образ. На Windows — подсказка (использовать WSL/imdisk/OSFMount).
    /// На Linux — сначала пробует umount без sudo, потом с sudo -n, потом с sudo (интерактивно).
    /// </summary>
    public static void MountImg(string imgPath, string mountPoint)
    {
        if (IsWindows)
        {
            Console.WriteLine("Автоматическое монтирование .img не поддерживается на Windows.");
            Console.WriteLine("Смонтируй образ вручную (через WSL / imdisk / OSFMount)");
            Console.WriteLine($"и убедись, что он доступен по пути: {mountPoint}");
            return;
        }

        try
        {
            Directory.CreateDirectory(mountPoint);

            string safeImg   = imgPath.Replace("'", "'\\''");
            string safeMount = mountPoint.Replace("'", "'\\''");

            // Порядок попыток:
            //   1. mount без sudo — сработает, если есть fstab-запись с опцией user
            //   2. sudo -n mount — если sudo-таймстамп ещё жив
            //   3. sudo mount — интерактивно спросит пароль
            string cmd =
                $"mount -- '{safeImg}' '{safeMount}' 2>/dev/null || " +
                $"sudo -n mount -- '{safeImg}' '{safeMount}' 2>/dev/null || " +
                $"sudo mount -- '{safeImg}' '{safeMount}'";

            var psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", cmd },
                UseShellExecute = false
            };
            Process.Start(psi)?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка монтирования: {ex.Message}");
        }
    }

    /// <summary>
    /// Размонтирует образ. Не падает, если что-то не так — данные уже синхронизированы.
    /// </summary>
    public static void Umount(string imgPath)
    {
        if (IsWindows) return;
        if (string.IsNullOrEmpty(imgPath)) return;

        try
        {
            // Сначала sync — сбрасываем буферы на диск
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", "sync" },
                UseShellExecute = false
            })?.WaitForExit();

            string safePath = imgPath.Replace("'", "'\\''");

            // Цепочка попыток:
            //   1. umount без sudo (для fstab с user)
            //   2. sudo -n umount (без запроса пароля)
            //   3. sudo umount (интерактивно)
            //   4. umount -l (lazy — отсоединяет, не дожидаясь процессов)
            string cmd =
                $"umount -- '{safePath}' 2>/dev/null || " +
                $"sudo -n umount -- '{safePath}' 2>/dev/null || " +
                $"sudo umount -- '{safePath}' 2>/dev/null || " +
                $"umount -l -- '{safePath}' 2>/dev/null || " +
                "true";

            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", cmd },
                UseShellExecute = false
            })?.WaitForExit();
        }
        catch { /* не критично */ }
    }

    /// <summary>
    /// Информационно: показывает, какие процессы держат точку монтирования.
    /// Не требует sudo, не мешает работе, используется только для диагностики.
    /// </summary>
    public static void CheckFuser(string mountPoint)
    {
        if (IsWindows) return;
        if (string.IsNullOrEmpty(mountPoint)) return;
        if (!Directory.Exists(mountPoint)) return;

        try
        {
            string safe = mountPoint.Replace("'", "'\\''");
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", $"fuser -vm '{safe}' 2>&1 || true" },
                UseShellExecute = false
            };
            Process.Start(psi)?.WaitForExit();
        }
        catch { /* не критично */ }
    }

    // ===================== Процессы =====================

    /// <summary>
    /// Запускает программу в новом терминале.
    /// Возвращает true, если удалось стартовать.
    /// </summary>
    public static bool LaunchInTerminal(string program, string argument)
    {
        if (string.IsNullOrEmpty(program))
        {
            Console.WriteLine("Путь к программе пуст.");
            return false;
        }

        try
        {
            if (IsWindows)
            {
                // Windows Terminal
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "wt.exe",
                        Arguments = $"-- \"{program}\" \"{argument}\"",
                        UseShellExecute = false
                    });
                    return true;
                }
                catch { /* wt не установлен */ }

                // Fallback: cmd /c start
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start \"\" \"{program}\" \"{argument}\"",
                        UseShellExecute = false
                    });
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка запуска терминала: {ex.Message}");
                    return false;
                }
            }

            // Linux / macOS — пробуем популярные терминалы
            var candidates = new (string term, string[] args)[]
            {
                ("gnome-terminal", new[] { "--", program, argument }),
                ("konsole",        new[] { "-e", program, argument }),
                ("xfce4-terminal", new[] { "--command", $"{program} \"{argument}\"" }),
                ("xterm",          new[] { "-e", program, argument }),
                ("alacritty",      new[] { "-e", program, argument }),
                ("kitty",          new[] { program, argument }),
                ("foot",           new[] { program, argument }),
            };

            foreach (var (term, args) in candidates)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = term,
                        UseShellExecute = false
                    };
                    foreach (var a in args) psi.ArgumentList.Add(a);
                    Process.Start(psi);
                    return true;
                }
                catch { /* пробуем следующий */ }
            }

            Console.WriteLine("Не найден ни один терминал.");
            Console.WriteLine("Установи gnome-terminal, konsole, xterm или xfce4-terminal.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запуска терминала: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ждёт, пока файл появится и его размер перестанет расти.
    /// </summary>
    public static bool WaitForFileStable(string path, int timeoutSec = 600)
    {
        var start = DateTime.Now;
        long lastSize = -1;
        int stable = 0;

        while ((DateTime.Now - start).TotalSeconds < timeoutSec)
        {
            if (File.Exists(path))
            {
                long size;
                try { size = new FileInfo(path).Length; }
                catch { size = -1; }

                if (size > 0 && size == lastSize)
                {
                    stable++;
                    if (stable >= 3) return true;
                }
                else
                {
                    stable = 0;
                }

                lastSize = size;
            }

            Thread.Sleep(500);
        }

        try
        {
            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
        catch { return false; }
    }

    // ===================== Открытие файлов =====================

    /// <summary>
    /// Открывает файл приложением по умолчанию.
    /// Windows  — ShellExecute (учитывает file associations).
    /// macOS    — open.
    /// Linux    — xdg-open.
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
