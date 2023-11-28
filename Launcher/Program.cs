using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Launcher
{
    public record Shortcut(string Name, string Path);
    public static class WindowsExplorerHelper
    {
        /// <param name="additionalArgs">Reserved for launching exes</param>
        public static void Launch(this string path, string[] additionalArgs = null)
        {
            if (!Directory.Exists(path) && !File.Exists(path) && !path.StartsWith("http"))
                throw new ArgumentException($"Invalid path: {path}");

            // Open with browser
            if (path.StartsWith("http"))
                path.OpenWithDefaultProgram();
            // Launch exe
            else if (path.EndsWith(".exe"))
                Process.Start(path, additionalArgs);
            // Open file/folder location
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    Arguments = $"/select,{path}", // Explorer will treat everything after /select as a path, so no quotes is necessasry and in fact, we shouldn't use quotes otherwise explorer will not work
                    FileName = "explorer.exe"
                });
            }
        }
        public static void OpenWithDefaultProgram(this string path)
        {
            new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{path}\""
                }
            }.Start();
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
                    lc --open: Open configuration folder
                    lc --config: Edit configuration with default editor
                    lc --list: List names
                    lc --search <keywords>: Search names/locations containing keywords
                    lc <Name>: Open shortcut
                    """);
            }
            else if (args.First() == "--config")
                ConfigurationPath.OpenWithDefaultProgram();
            else if (args.First() == "--open")
                ConfigurationPath.Launch();
            else if (args.First() == "--search")
            {
                if (args.Length != 2)
                    Console.WriteLine("Invalid number of arguments.");
                else
                {
                    string keywords = args.Last();
                    foreach (Shortcut item in ReadConfigurations().Values)
                        if (Regex.IsMatch(item.Name, keywords, RegexOptions.IgnoreCase) || Regex.IsMatch(item.Path, keywords, RegexOptions.IgnoreCase))
                            Console.WriteLine($"{item.Name}: {item.Path}");
                }
            }
            else if (args.First() == "--list")
                foreach (Shortcut item in ReadConfigurations().Values)
                    Console.WriteLine($"{item.Name}: {item.Path}");
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
                .Select(line =>
                {
                    int splitter = line.IndexOf(':');
                    string name = line.Substring(0, splitter).Trim();
                    string value = line.Substring(splitter + 1).Trim();
                    return new Shortcut(name, value);
                })
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
