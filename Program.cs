using CertShell.Config;
using CertShell.Platform;
using CertShell.Security;
using CertShell.Commands;
using CertShell.FileSelector;

PlatformHelper.EnsureDataDirs();

if (args.Contains("--setup") || args.Contains("--init"))
{
    SetupWizard.Run();
    Environment.Exit(0);
}

if (!AppConfig.Exists())
    SetupWizard.Run();

RegisterCleanup();
StartupCleanup();
CheckSecurity.Init();

while (true)
{
    Console.Write("kirill@archlinux -> ");
    string? commandeLine = Console.ReadLine();
    if (commandeLine == null) break;

    try
    {
        if (commandeLine != "open")
        {
            if (!File.Exists(Path.Combine(PlatformHelper.CacheDir, "cd")))
                Commande.CallOfSystemCommands(commandeLine);
            else
                ChangeDirectory.CallOfSystemCommands(commandeLine);
        }
        else
        {
            if (!File.Exists(Path.Combine(PlatformHelper.CacheDir, "cd")))
                CallOfCertguard.OpenSelected();
            else
                CallOfCertguardForChangeDirectory.OpenSelected();
        }

        if (commandeLine == "cd")
        {
            File.WriteAllText(Path.Combine(PlatformHelper.CacheDir, "cd"), "1");
            ChangeDirectory.MoveToDirectory();
        }

        if (commandeLine == "cd ..")
        {
            Commande.RemoveCache();
            ChangeDirectory.RemoveCache();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

Environment.Exit(0);

// ============================================================
//  Cleanup
// ============================================================

static void StartupCleanup()
{
    try
    {
        string mount = AppConfig.Instance.MountPoint;
        if (!Directory.Exists(mount)) return;

        int removed = 0;

        void ScanAndRemove(string dir)
        {
            try
            {
                foreach (var f in Directory.GetFiles(dir, "*.mp4"))
                {
                    try { File.Delete(f); removed++; } catch { }
                }
                foreach (var d in Directory.GetDirectories(dir))
                    ScanAndRemove(d);
            }
            catch { }
        }

        ScanAndRemove(mount);

        if (removed > 0)
            AuditLog.Info($"startup_cleanup removed={removed}");
    }
    catch { }
}

static void RegisterCleanup()
{
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        RunExitCleanup();
    };

    AppDomain.CurrentDomain.ProcessExit += (_, _) => RunExitCleanup();
    AppDomain.CurrentDomain.UnhandledException += (_, _) => RunExitCleanup();
}

static void RunExitCleanup()
{
    try
    {
        try
        {
            string mount = AppConfig.Instance.MountPoint;
            if (Directory.Exists(mount))
            {
                void ScanAndRemove(string dir)
                {
                    try
                    {
                        foreach (var f in Directory.GetFiles(dir, "*.mp4"))
                            try { File.Delete(f); } catch { }
                        foreach (var d in Directory.GetDirectories(dir))
                            ScanAndRemove(d);
                    }
                    catch { }
                }
                ScanAndRemove(mount);
            }
        }
        catch { }

        try
        {
            Commande.RemoveCache();
            ChangeDirectory.RemoveCache();
        }
        catch { }

        AuditLog.Info("session_cleanup");
    }
    catch { }
}
