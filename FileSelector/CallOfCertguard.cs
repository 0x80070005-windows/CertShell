using CertShell.Commands;
using CertShell.Config;

namespace CertShell.FileSelector;

public static class CallOfCertguard
{
    public static void GetListFiles(string folderPath)
    {
        Directory.CreateDirectory(Platform.PlatformHelper.CacheDir);

        string[] files = Directory.Exists(folderPath)
            ? Directory.GetFileSystemEntries(folderPath)
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToArray()
            : Array.Empty<string>();

        File.WriteAllLines(
            Path.Combine(Platform.PlatformHelper.CacheDir, "file_list.txt"),
            files);
    }

    public static void SelectRequiredElement()
    {
        string listPath = Path.Combine(Platform.PlatformHelper.CacheDir, "file_list.txt");
        string? selected = FileSelectorUi.Select(listPath);
        if (selected == null) return;

        if (selected.EndsWith(".enc", StringComparison.Ordinal))
            Commande.LaunchCertguard(selected);
        else
            DefiningExtensions.OpenFile(Path.Combine(AppConfig.Instance.MountPoint, selected));

        if (File.Exists(listPath))
            File.Delete(listPath);
    }

    public static void OpenSelected()
    {
        GetListFiles(AppConfig.Instance.MountPoint);
        SelectRequiredElement();
    }
}
