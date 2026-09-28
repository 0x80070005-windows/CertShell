using CertShell.Platform;

namespace CertShell.Security;

/// <summary>
/// Простой append-only журнал событий безопасности.
/// Файл: $DataDir/audit.log (права 0600).
/// </summary>
public static class AuditLog
{
    private static string LogFile => Path.Combine(PlatformHelper.DataDir, "audit.log");
    private static readonly object Lock = new();

    public static void Info(string message)  => Write("INFO",  message);
    public static void Warn(string message)  => Write("WARN",  message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(PlatformHelper.DataDir);
            string line = $"{DateTime.UtcNow:O} [{level}] {message}{Environment.NewLine}";

            lock (Lock)
            {
                File.AppendAllText(LogFile, line);

                if (!PlatformHelper.IsWindows)
                {
                    try { File.SetUnixFileMode(LogFile, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
                    catch { }
                }
            }
        }
        catch { /* лог не должен ломать программу */ }
    }
}
