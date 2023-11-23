using System.Diagnostics;

namespace Launcher
{
    public record Shortcut(string Name, string Path);
    public static class WindowsExplorerHelper
    {
        public static void Launch(this string path)
        {
            if (!Directory.Exists(path) || !File.Exists(path))
                throw new ArgumentException($"Invalid path: {path}");
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    Arguments = $"/select,{path}", // Explorer will treat everything after /select as a path, so no quotes is necessasry and in fact, we shouldn't use quotes otherwise explorer will not work
                    FileName = "explorer.exe"
                });
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var configurations = ReadConfigurations();
            string name = args.First();
            if (configurations.TryGetValue(name, out Shortcut shortcut))
            {
                try
                {
                    shortcut.Path.Launch();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine($"Configuration {name} not found");
                Console.ReadKey();
            }
        }
        static Dictionary<string, Shortcut> ReadConfigurations()
        {
            string configurationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Launcher");
            string configurationPath = Path.Combine(configurationFolder, "Configurations.yaml");
            Directory.CreateDirectory(configurationFolder);
            if (!File.Exists(configurationPath))
                File.WriteAllText(configurationPath, "# Configurations");

            return File.ReadLines(configurationPath)
                .Where(line => !line.StartsWith('#'))
                .Select(line => line.Split(':', StringSplitOptions.TrimEntries))
                .Select(parts => new Shortcut(parts.First(), parts.Last()))
                .ToDictionary(shortcut => shortcut.Name, shortcut => shortcut);
        }
    }
}
