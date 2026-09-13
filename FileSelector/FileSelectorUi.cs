namespace CertShell.FileSelector;

public static class FileSelectorUi
{
    /// <summary>
    /// Показывает список и возвращает выбранный элемент.
    /// null — если пользователь прервал (Ctrl+C) или список пуст.
    /// </summary>
    public static string? Select(string listFilePath)
    {
        if (!File.Exists(listFilePath)) return null;

        string[] fileList = File.ReadAllLines(listFilePath)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToArray();

        if (fileList.Length == 0)
        {
            Console.Clear();
            Console.WriteLine("Список пуст.");
            return null;
        }

        int currentIndex = 0;
        int visibleCount = 1;
        int maxCount = fileList.Length;

        void Render()
        {
            Console.Clear();
            for (int i = 0; i < visibleCount && i < maxCount; i++)
            {
                string prefix = (i == currentIndex) ? "-> " : "   ";
                Console.WriteLine(prefix + fileList[i]);
            }
        }

        Render();

        while (true)
        {
            if (!Console.KeyAvailable)
            {
                Thread.Sleep(10);
                continue;
            }

            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Tab)
            {
                if (currentIndex < maxCount - 1)
                {
                    currentIndex++;
                    if (currentIndex >= visibleCount)
                        visibleCount = currentIndex + 1;
                }
                else
                {
                    currentIndex = maxCount - 1;
                }
                Render();
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                Console.Clear();
                Console.WriteLine("Выбранный элемент:");
                Console.WriteLine(fileList[currentIndex]);
                return fileList[currentIndex];
            }
            else if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control)
            {
                Console.Clear();
                Console.WriteLine("Interrupted by user.");
                return null;
            }
        }
    }
}
