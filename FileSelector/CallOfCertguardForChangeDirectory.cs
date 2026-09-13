using CertShell.Commands;
using CertShell.Config;

namespace CertShell.FileSelector;

public static class CallOfCertguardForChangeDirectory
{
    public static void GetListFiles()
    {
        string nameDirectory = File.ReadAllText("Cache/name_directory").Trim();
        string folderPath = Path.Combine(AppConfig.Instance.MountPoint, nameDirectory);

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
        {
            ChangeDirectory.LaunchCertguard(selected);
        }
        else
        {
            string nameDirectory = File.ReadAllText("Cache/name_directory").Trim();
            string fullPath = Path.Combine(AppConfig.Instance.MountPoint, nameDirectory, selected);
            DefiningExtensions.Main(fullPath);
        }

        if (File.Exists("Cache/file_list.txt"))
            File.Delete("Cache/file_list.txt");
    }

    public static void Main()
    {
        GetListFiles();
        SelectRequiredElement();
    }
}
