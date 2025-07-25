using ConsoleTables;
using Launcher.Shared;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Launcher
{
    public static class Program
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
                File.AppendAllText(LauncherCore.ConfigurationPath, $"\n{args[1]}: {args[2]}");
            else if (args.First().ToLower() == "--edit" || args.First().ToLower() == "-e")
                LauncherCore.ConfigurationPath.OpenWithDefaultProgram(null);
            else if (args.First().ToLower() == "--dir" || args.First().ToLower() == "-d")
                LauncherCore.ConfigurationPath.Launch();
            else if (args.First().ToLower() == "--search" || args.First().ToLower() == "-s")
            {
                if (args.Length != 2)
                    Console.WriteLine("Invalid number of arguments.");
                else
                {
                    string keywords = args.Last();
                    LauncherCore.PrintAsTable(LauncherCore.ReadConfigurations().Values
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
                    Shortcut item = LauncherCore.ReadConfigurations().Values
                        .SingleOrDefault(item => item.Name == name);
                    if (item == null)
                        Console.WriteLine($"{name} is not defined.");
                    else
                    Console.WriteLine(item.Path);
                }
            }
            else if (args.First().ToLower() == "--list" || args.First().ToLower() == "-l")
                LauncherCore.PrintAsTable(LauncherCore.ReadConfigurations().Values);
            else if (args.First().ToLower() == "--open" || args.First().ToLower() == "-o")
                LauncherCore.Launch(args[1], args.Skip(2).ToArray(), true);
            else if (args.First().ToLower().StartsWith("--") || args.First().ToLower().StartsWith("-"))
                Console.WriteLine($"Invalid argument: {args.First()}");
            else
                LauncherCore.Launch(args.First(), args.Skip(1).ToArray(), false);
        }
        #endregion
    }
}
