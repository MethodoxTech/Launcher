using System.Diagnostics;

namespace Launcher
{
    public record Shortcut(string Name, string Path);
    public static class WindowsExplorerHelper
    {
        /// <param name="additionalArgs">Reserved for launching exes</param>
        public static void Launch(this string path, string[] additionalArgs = null)
        {
            if (!Directory.Exists(path) && !File.Exists(path))
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
        #region Entrance
        static void Main(string[] args)
        {
            if (args.Length == 0 || args.First() == "--help")
            {
                Console.WriteLine("""
                    lc --help: Print help
                    lc --config: Open configuration folder
                    lc <Name>: Open shortcut
                    """);
            }
            else if (args.First() == "--config")
                ConfigurationPath.Launch();
            else
                Launch(args.First(), args.Skip(1).ToArray());
        }
        #endregion

        #region Routines
        private static void Launch(string name, string[] args)
        {
            var configurations = ReadConfigurations();
            if (configurations.TryGetValue(name, out Shortcut shortcut))
            {
                try
                {
                    shortcut.Path.Launch(args);
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
            return File.ReadLines(ConfigurationPath)
                .Where(line => !line.StartsWith('#'))
                .Select(line => line.Split(':', StringSplitOptions.TrimEntries))
                .Select(parts => new Shortcut(parts.First(), parts.Last()))
                .ToDictionary(shortcut => shortcut.Name, shortcut => shortcut);
        }
        #endregion

        #region Helpers
        public static string ConfigurationFolder
        {
            get
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Launcher");
                Directory.CreateDirectory(path);
                return path;
            }
        }
        public static string ConfigurationPath
        {
            get
            {
                string path = Path.Combine(ConfigurationFolder, "Configurations.yaml"); ;
                if (!File.Exists(path))
                    File.WriteAllText(path, """
                        # Format: <Name>: <Path>
                        # Configurations
                        """);
                return path;
            }
        }
        #endregion
    }
}
