using CertShell.Commands;
using CertShell.Config;

namespace CertShell.FileSelector;

public static class CallOfCertguard
{
    public static void GetListFiles(string folderPath)
    {
        Directory.CreateDirectory("Cache");

        string[] files = Directory.Exists(folderPath)
            ? Directory.GetFileSystemEntries(folderPath)
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToArray()
            : Array.Empty<string>();

        File.WriteAllLines("Cache/file_list.txt", files);
    }

    public static void SelectRequiredElement()
    {
        string? selected = FileSelectorUi.Select("Cache/file_list.txt");
        if (selected == null) return;

        if (selected.EndsWith(".enc", StringComparison.Ordinal))
            Commande.LaunchCertguard(selected);
        else
            DefiningExtensions.Main(Path.Combine(AppConfig.Instance.MountPoint, selected));

        if (File.Exists("Cache/file_list.txt"))
            File.Delete("Cache/file_list.txt");
    }

    public static void Main()
    {
        GetListFiles(AppConfig.Instance.MountPoint);
        SelectRequiredElement();
    }
}
