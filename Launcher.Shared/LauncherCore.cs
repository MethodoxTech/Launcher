using ConsoleTables;
using Divooka.Core.Helpers;

namespace Launcher.Shared
{
    public static class LauncherCore
    {
        #region Routines
        public static void Launch(string name, string[] args, bool launchWithDefaultProgram)
        {
            Dictionary<string, LaunchOption> configurations = ReadConfigurations();
            if (configurations.TryGetValue(name, out LaunchOption shortcut))
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
        public static Dictionary<string, LaunchOption> ReadConfigurations()
        {
            return File.ReadLines(ConfigurationPath)
                .Where(line => !line.StartsWith('#') && !string.IsNullOrWhiteSpace(line))   // Skip comment and empty lines
                .Select(ParseShortcut)
                .ToDictionary(shortcut => shortcut.Name, shortcut => shortcut);
        }
        #endregion

        #region Helpers
        public static LaunchOption ParseShortcut(string line)
        {
            int splitter = line.IndexOf(':');
            string name = line.Substring(0, splitter).Trim();
            string value = line.Substring(splitter + 1).Trim();
            return new LaunchOption(name, value.Trim('\"'));
        }
        public static void PrintAsTable(IEnumerable<LaunchOption> items)
        {
            ConsoleTable table = new("Name", "Type", "Path");
            foreach (LaunchOption item in items)
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
