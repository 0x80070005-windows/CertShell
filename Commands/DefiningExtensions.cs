using CertShell.Platform;
using System.Diagnostics;

namespace CertShell.Commands;

public static class DefiningExtensions
{
    private static readonly HashSet<string> PictureExts = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp" };

    private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".avi", ".mkv", ".mov" };

    private static readonly HashSet<string> TextExts = new(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".log" };

    public static void OpenPicture(string path)
    {
        if (PlatformHelper.IsWindows || PlatformHelper.IsMacOS)
            PlatformHelper.OpenWithDefault(path);   // Paint / Preview / Photos
        else
            RunLinux("eog", path);
    }

    public static void OpenVideo(string path)
    {
        if (PlatformHelper.IsWindows || PlatformHelper.IsMacOS)
            PlatformHelper.OpenWithDefault(path);   // Windows Media Player / QuickTime
        else
            RunLinux("mpv", path);
    }

    public static void OpenText(string path)
    {
        if (PlatformHelper.IsWindows)
            RunWindows("notepad.exe", path);
        else if (PlatformHelper.IsMacOS)
            PlatformHelper.OpenWithDefault(path);   // TextEdit
        else
            RunLinux("gedit", path);
    }

    private static void RunLinux(string program, string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = program,
                ArgumentList = { path },
                UseShellExecute = false
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запуска {program}: {ex.Message}");
        }
    }

    private static void RunWindows(string program, string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = program,
                ArgumentList = { path },
                UseShellExecute = false
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка запуска {program}: {ex.Message}");
        }
    }

    public static void NotSupportedExtensions(string path)
    {
        Console.WriteLine("1 - video");
        Console.WriteLine("2 - picture");
        Console.WriteLine("3 - text");
        Console.WriteLine("4 - exit");

        while (true)
        {
            Console.Write("какой это тип файла - ");
            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1": OpenVideo(path);   return;
                case "2": OpenPicture(path); return;
                case "3": OpenText(path);    return;
                case "4": return;
            }
        }
    }

    public static void Main(string selectedFile)
    {
        string realFile = selectedFile.EndsWith(".enc", StringComparison.Ordinal)
            ? selectedFile[..^4]
            : selectedFile;

        string ext = Path.GetExtension(realFile);

        if (PictureExts.Contains(ext))      OpenPicture(realFile);
        else if (VideoExts.Contains(ext))   OpenVideo(realFile);
        else if (TextExts.Contains(ext))    OpenText(realFile);
        else                                NotSupportedExtensions(realFile);
    }
}
