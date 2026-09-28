using CertShell.Platform;
using System.Text.Json;

namespace CertShell.Config;

public class AppConfig
{
    public string CertPath      { get; set; } = "";
    public string ImgPath       { get; set; } = "";
    public string MountPoint    { get; set; } = PlatformHelper.DefaultMountPoint;
    public string CertGuardPath { get; set; } = "CertGuard";

    private static string ConfigDir  => PlatformHelper.DataDir;
    private static string ConfigFile => Path.Combine(ConfigDir, "config.json");

    private static AppConfig? _instance;

    public static AppConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                if (!File.Exists(ConfigFile))
                    throw new InvalidOperationException("Config not found");

                var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFile))
                    ?? throw new InvalidOperationException("Invalid config");

                if (string.IsNullOrEmpty(cfg.CertPath))
                    throw new InvalidOperationException("CertPath missing");
                if (string.IsNullOrEmpty(cfg.ImgPath))
                    throw new InvalidOperationException("ImgPath missing");
                if (string.IsNullOrEmpty(cfg.MountPoint))
                    throw new InvalidOperationException("MountPoint missing");
                if (string.IsNullOrEmpty(cfg.CertGuardPath))
                    cfg.CertGuardPath = "CertGuard";

                _instance = cfg;
            }
            return _instance;
        }
    }

    public static bool Exists() => File.Exists(ConfigFile);

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
        _instance = config;

        if (!PlatformHelper.IsWindows)
        {
            try { File.SetUnixFileMode(ConfigFile, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
            catch { }
        }
    }
}
