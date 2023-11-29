using ConsoleTables;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Launcher
{
    public enum ShortcutType
    {
        Executable,
        DiskLocation,
        URL
    }
    public record Shortcut(string Name, string Path)
    {
        #region Properties
        public ShortcutType Type => GetShortcutType(Path);
        public bool IsURL => Type == ShortcutType.URL;
        public bool IsExecutable => Type == ShortcutType.Executable;
        #endregion

        #region Helper
        public static ShortcutType GetShortcutType(string path)
        {
            if (path.StartsWith("http"))
                return ShortcutType.URL;
            else if (path.EndsWith(".exe"))
                return ShortcutType.Executable;
            else
                return ShortcutType.DiskLocation;
        }
        #endregion
    }
    public static class WindowsExplorerHelper
    {
        /// <param name="additionalArgs">Reserved for launching exes</param>
        public static void Launch(this string path, string[] additionalArgs = null)
        {
            if (!Directory.Exists(path) && !File.Exists(path) && !path.StartsWith("http"))
                throw new ArgumentException($"Invalid path: {path}");

            switch (Shortcut.GetShortcutType(path))
            {
                case ShortcutType.Executable:
                    // Launch exe
                    Process.Start(path, additionalArgs);
                    break;
                case ShortcutType.DiskLocation:
                    // Open file/folder location
                    Process.Start(new ProcessStartInfo
                    {
                        Arguments = $"/select,{path}", // Explorer will treat everything after /select as a path, so no quotes is necessasry and in fact, we shouldn't use quotes otherwise explorer will not work
                        FileName = "explorer.exe"
                    });
                    break;
                case ShortcutType.URL:
                    // Open with browser
                    path.OpenWithDefaultProgram();
                    break;
                default:
                    break;
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
            if (args.Length == 0 || args.First() == "--help" || args.First() == "-h")
            {
                Console.WriteLine("""
                    Launcher by Charles Zhang
                      Launches shortcuts by name as defined in configuration file, for disk locations, urls, and executables.

                    Basic commands:
                      lc --help: Print help
                      lc --dir: Open configuration folder
                      lc --config: Edit configuration with default editor
                      lc --list: List names
                      lc --search <keywords>: Search names/locations containing keywords
                      lc <Name>: Open shortcut

                    Additional commands:
                      lc --print <Name>: Print path of shortcut (useful in shell and with other programs)
                    """.TrimEnd());
            }
            else if (args.First().ToLower() == "--config" || args.First().ToLower() == "-c")
                ConfigurationPath.OpenWithDefaultProgram();
            else if (args.First().ToLower() == "--dir" || args.First().ToLower() == "-d")
                ConfigurationPath.Launch();
            else if (args.First().ToLower() == "--search" || args.First().ToLower() == "-s")
            {
                if (args.Length != 2)
                    Console.WriteLine("Invalid number of arguments.");
                else
                {
                    string keywords = args.Last();
                    PrintAsTable(ReadConfigurations().Values
                        .Where(item => Regex.IsMatch(item.Name, keywords, RegexOptions.IgnoreCase) || Regex.IsMatch(item.Path, keywords, RegexOptions.IgnoreCase)));
                }
            }
            else if (args.First().ToLower() == "--print" || args.First().ToLower() == "-p")
            {
                if (args.Length != 2)
                    Console.WriteLine("Invalid number of arguments.");
                else
                {
                    string name = args.Last();
                    Shortcut item = ReadConfigurations().Values
                        .SingleOrDefault(item => item.Name == name);
                    if (item == null)
                        Console.WriteLine($"{name} is not defined.");
                    else
                    Console.WriteLine(item.Path);
                }
            }
            else if (args.First().ToLower() == "--list" || args.First().ToLower() == "-l")
                PrintAsTable(ReadConfigurations().Values);
            else if (args.First().ToLower().StartsWith("--") || args.First().ToLower().StartsWith("-"))
                Console.WriteLine($"Invalid argument: {args.First()}");
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
                Console.WriteLine($"Shortcut {name} is not defined.");
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
        public static void PrintAsTable(IEnumerable<Shortcut> items)
        {
            var table = new ConsoleTable("Name", "Type", "Path");
            foreach (Shortcut item in items)
                table.AddRow(item.Name, item.Type, item.Path);
            table.Write(Format.Minimal);
        }
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
