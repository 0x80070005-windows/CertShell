using CertShell.Config;
using CertShell.Security;
using CertShell.Commands;
using CertShell.FileSelector;

Directory.CreateDirectory("Cache");

try
{
    if (args.Contains("--setup") || args.Contains("--init"))
    {
        SetupWizard.Run();
        Environment.Exit(0);
    }

    if (!AppConfig.Exists())
    {
        SetupWizard.Run();
    }

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
                if (!File.Exists("Cache/cd"))
                    Commande.CallOfSystemCommands(commandeLine);
                else
                    ChangeDirectory.CallOfSystemCommands(commandeLine);
            }
            else
            {
                if (!File.Exists("Cache/cd"))
                    CallOfCertguard.Main();
                else
                    CallOfCertguardForChangeDirectory.Main();
            }

            if (commandeLine == "cd")
            {
                File.WriteAllText("Cache/cd", "1");
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
}
catch
{
    Environment.Exit(0);
}
