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

    private static readonly HashSet<string> ArchiveExts = new(StringComparer.OrdinalIgnoreCase)
        { ".zip" , ".rar" , ".tar" , ".tgz" , ".iso" , ".img"};

    public static void OpenPicture(string path)
    {
        if (PlatformHelper.IsWindows || PlatformHelper.IsMacOS)
            PlatformHelper.OpenWithDefault(path);
        else
            RunLinux("eog", path);
    }

    public static void OpenVideo(string path)
    {
        if (PlatformHelper.IsWindows || PlatformHelper.IsMacOS)
            PlatformHelper.OpenWithDefault(path);
        else
            RunLinux("mpv", path);
    }

    public static void OpenText(string path)
    {
        if (PlatformHelper.IsWindows)
            RunWindows("notepad.exe", path);
        else if (PlatformHelper.IsMacOS)
            PlatformHelper.OpenWithDefault(path);
        else
            RunLinux("gedit", path);
    }

    public static void OpenArchive(string path)
    {
        if (PlatformHelper.IsWindows || PlatformHelper.IsMacOS)
            PlatformHelper.OpenWithDefault(path);
        else
            RunLinux("peazip", path);
            
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
        Console.WriteLine("4 - Archive");
        Console.WriteLine("5 - exit");

        while (true)
        {
            Console.Write("какой это тип файла - ");
            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1": OpenVideo(path);   return;
                case "2": OpenPicture(path); return;
                case "3": OpenText(path);    return;
                case "4": OpenArchive(path); return;
                case "5": return;
            }
        }
    }

    /// <summary>
    /// Открывает файл по расширению. Раньше назывался Main (конфликт с top-level Program).
    /// </summary>
    public static void OpenFile(string selectedFile)
    {
        string realFile = selectedFile.EndsWith(".enc", StringComparison.Ordinal)
            ? selectedFile[..^4]
            : selectedFile;

        string ext = Path.GetExtension(realFile);

        if (PictureExts.Contains(ext))      OpenPicture(realFile);
        else if (VideoExts.Contains(ext))   OpenVideo(realFile);
        else if (TextExts.Contains(ext))    OpenText(realFile);
        else if (ArchiveExts.Contains(ext)) OpenArchive(realFile);
        else                                NotSupportedExtensions(realFile);
    }
}
