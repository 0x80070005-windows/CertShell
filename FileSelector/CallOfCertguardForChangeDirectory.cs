using CertShell.Commands;
using CertShell.Config;
using CertShell.Platform;

namespace CertShell.FileSelector;

public static class CallOfCertguardForChangeDirectory
{
    private static string ListFile => Path.Combine(PlatformHelper.CacheDir, "file_list.txt");
    private static string NameDirFile => Path.Combine(PlatformHelper.CacheDir, "name_directory");

    public static void GetListFiles()
    {
        string nameDirectory;
        try { nameDirectory = File.ReadAllText(NameDirFile).Trim(); }
        catch { return; }

        string folderPath = Path.Combine(AppConfig.Instance.MountPoint, nameDirectory);
        Directory.CreateDirectory(PlatformHelper.CacheDir);

        string[] files = Directory.Exists(folderPath)
            ? Directory.GetFileSystemEntries(folderPath)
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToArray()
            : Array.Empty<string>();

        File.WriteAllLines(ListFile, files);
    }

    public static void SelectRequiredElement()
    {
        string? selected = FileSelectorUi.Select(ListFile);
        if (selected == null) return;

        if (selected.EndsWith(".enc", StringComparison.Ordinal))
        {
            ChangeDirectory.LaunchCertguard(selected);
        }
        else
        {
            string nameDirectory = File.ReadAllText(NameDirFile).Trim();
            string fullPath = Path.Combine(AppConfig.Instance.MountPoint, nameDirectory, selected);
            DefiningExtensions.OpenFile(fullPath);
        }

        if (File.Exists(ListFile))
            File.Delete(ListFile);
    }

    public static void OpenSelected()
    {
        GetListFiles();
        SelectRequiredElement();
    }
}
