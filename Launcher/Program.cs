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
        public static void Launch(this string path, string[] additionalArgs = null, bool launchWithDefaultProgram = false)
        {
            // Verbatim commands
            if (path.StartsWith('!'))
            {
                path = path[1..];
                bool captureOutputs = false;
                if (path.StartsWith('?'))
                {
                    path = path[1..];
                    captureOutputs = true;
                }

                string filename = path.Split(' ').First(); // TODO: Handle with the case that there are spaces in the filename
                string arguments = path[filename.Length..];
                if (captureOutputs)
                    MonitorProcess(filename, arguments);
                else
                    Process.Start(filename, arguments);
                return;
            }

            if (!Directory.Exists(path) && !File.Exists(path) && !path.StartsWith("http"))
                throw new ArgumentException($"Invalid path: {path}");

            switch (Shortcut.GetShortcutType(path))
            {
                case ShortcutType.Executable:
                    // Launch exe
                    Process.Start(path, additionalArgs);
                    break;
                case ShortcutType.DiskLocation:
                    if (launchWithDefaultProgram)
                        // Open with default program
                        path.OpenWithDefaultProgram(additionalArgs);
                    else
                        // Open file/folder location
                        Process.Start(new ProcessStartInfo
                        {
                            Arguments = $"/select,{path}", // Explorer will treat everything after /select as a path, so no quotes is necessasry and in fact, we shouldn't use quotes otherwise explorer will not work
                            FileName = "explorer.exe"
                        });
                    break;
                case ShortcutType.URL:
                    // Open with browser
                    path.OpenWithDefaultProgram(additionalArgs);
                    break;
                default:
                    break;
            }
        }
        public static void OpenWithDefaultProgram(this string path, string[] additionalArgs)
        {
            new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = additionalArgs == null
                        ? $"\"{path}\""
                        : $"\"{path}\" {EscapeArguments(additionalArgs)}"
                }
            }.Start();

            string EscapeArguments(string[] arguments)
                => string.Join(" ", arguments.Select(argument => argument.Contains(' ') ? $"\"{argument}\"" : argument));
        }
        private static void MonitorProcess(string filename, string arguments)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = filename,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true,
            };
            process.ErrorDataReceived += OutputDataReceived;
            process.OutputDataReceived += OutputDataReceived;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            static void OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                string message = e.Data;
                Console.WriteLine(message);
            }
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
                      lc --edit: Edit configuration with default editor
                      lc --create: Create or update existing entries (partially implemented)
                      lc --list: List names
                      lc --search <keywords>: Search names/locations containing keywords
                      lc <Name> [<Arguments>...]: Open shortcut

                    Additional commands:
                      lc --print <Name>: Print path of shortcut (useful in shell and with other programs)
                      lc --open <Name> [<Arguments>...]: Open file with default program; Open other links with browser
                    """.TrimEnd());
            }
            else if (args.First().ToLower() == "--create" || args.First().ToLower() == "-c")
                File.AppendAllText(ConfigurationPath, $"\n{args[1]}: {args[2]}");
            else if (args.First().ToLower() == "--edit" || args.First().ToLower() == "-e")
                ConfigurationPath.OpenWithDefaultProgram(null);
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
            else if (args.First().ToLower() == "--open" || args.First().ToLower() == "-o")
                Launch(args[1], args.Skip(2).ToArray(), true);
            else if (args.First().ToLower().StartsWith("--") || args.First().ToLower().StartsWith("-"))
                Console.WriteLine($"Invalid argument: {args.First()}");
            else
                Launch(args.First(), args.Skip(1).ToArray(), false);
        }
        #endregion

        #region Routines
        private static void Launch(string name, string[] args, bool launchWithDefaultProgram)
        {
            var configurations = ReadConfigurations();
            if (configurations.TryGetValue(name, out Shortcut shortcut))
            {
                try
                {
                    shortcut.Path.Launch(args, launchWithDefaultProgram);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine($"Shortcut {name} is not defined.");
            }
        }
        static Dictionary<string, Shortcut> ReadConfigurations()
        {
            return File.ReadLines(ConfigurationPath)
                .Where(line => !line.StartsWith('#') && !string.IsNullOrWhiteSpace(line))   // Skip comment and empty lines
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
                        # Notes:
                        #   Use ! to start verbatim
                        #   Use !? to monitor process outputs

                        # Configurations
                        """);
                return path;
            }
        }
        #endregion
    }
}
